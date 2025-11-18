using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public class LocalizationSyncManager
{
    private FeishuService _feishuService;
    private Dictionary<string, Dictionary<string, string>> _localizations;
    private StringTableCollection _stringTableCollection;
    private Dictionary<StringTableCollection, Dictionary<string, Dictionary<string, string>>> _allLocalizations;

    public LocalizationSyncManager()
    {
        _feishuService = new FeishuService();
        _localizations = new Dictionary<string, Dictionary<string, string>>();
        _allLocalizations = new Dictionary<StringTableCollection, Dictionary<string, Dictionary<string, string>>>();
    }

    public async Task LoadLocalizations()
    {
        try
        {
            Debug.Log("[本地化同步] 开始加载本地化数据...");
            _allLocalizations.Clear();
            
            // 获取所有本地化表
            await Task.Yield(); // 确保在主线程上执行
            var stringTables = LocalizationEditorSettings.GetStringTableCollections();
            if (stringTables.Count == 0)
            {
                Debug.LogWarning("[本地化同步] 未找到本地化表，请先创建本地化表");
                return;
            }

            Debug.Log($"[本地化同步] 找到 {stringTables.Count} 个本地化表集合");
            var locales = LocalizationEditorSettings.GetLocales();
            Debug.Log($"[本地化同步] 找到 {locales.Count} 个语言");

            // 处理每个本地化表
            foreach (var tableCollection in stringTables)
            {
                Debug.Log($"[本地化同步] 处理本地化表: {tableCollection.TableCollectionName}");
                var tableData = new Dictionary<string, Dictionary<string, string>>();
                _allLocalizations[tableCollection] = tableData;

                // 遍历所有语言
                foreach (var locale in locales)
                {
                    Debug.Log($"[本地化同步] 处理语言: {locale.Identifier.Code}");
                    var table = tableCollection.GetTable(locale.Identifier) as StringTable;
                    if (table == null)
                    {
                        Debug.LogWarning($"[本地化同步] 未找到语言 {locale.Identifier.Code} 的字符串表");
                        continue;
                    }

                    // 遍历所有条目
                    int entryCount = 0;
                    foreach (var entry in table.SharedData.Entries)
                    {
                        if (!tableData.ContainsKey(entry.Key))
                        {
                            tableData[entry.Key] = new Dictionary<string, string>();
                        }

                        var value = table.GetEntry(entry.Id)?.Value;
                        if (!string.IsNullOrEmpty(value))
                        {
                            tableData[entry.Key][locale.Identifier.Code] = value;
                            entryCount++;
                        }
                    }
                    Debug.Log($"[本地化同步] 语言 {locale.Identifier.Code} 加载了 {entryCount} 个翻译条目");
                }
                Debug.Log($"[本地化同步] 表 {tableCollection.TableCollectionName} 加载完成，共 {tableData.Count} 个键");
            }

            Debug.Log($"[本地化同步] 本地化数据加载完成，共 {_allLocalizations.Count} 个表");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[本地化同步] 加载本地化数据时发生错误: {ex}");
            throw;
        }
    }

    private async Task<string> EnsureTableExists(StringTableCollection tableCollection)
    {
        try
        {
            var tableName = tableCollection.TableCollectionName;
            Debug.Log($"[本地化同步] 确保表格存在: {tableName}");
            
            // 尝试查找与本地化表同名的表格
            var tables = await _feishuService.ListTables();
            
            foreach (var table in tables)
            {
                if (table["name"].Value<string>() == tableName)
                {
                    var foundTableId = table["table_id"].Value<string>();
                    Debug.Log($"[本地化同步] 找到匹配的表格: {tableName}, ID: {foundTableId}");
                    
                    // 确保所有必需的字段都存在
                    var locales = LocalizationEditorSettings.GetLocales();
                    var requiredFields = new List<(string name, int type)>
                    {
                        ("Key", 1),
                        ("Description", 1),
                        ("Status", 3),
                        ("ReviewStatus", 1)
                    };
                    requiredFields.AddRange(locales.Select(l => (l.Identifier.Code, 1)));
                    requiredFields.AddRange(locales.Select(l => ($"Review_{l.Identifier.Code}", 1)));
                    
                    await _feishuService.EnsureFieldsExist(foundTableId, requiredFields);
                    return foundTableId;
                }
            }

            // 如果没有找到匹配的表格，创建新表格
            Debug.Log($"[本地化同步] 未找到匹配的表格，创建新表格: {tableName}");
            var result = await _feishuService.CreateTable(tableName);
            var newTableId = result["data"]["table_id"].Value<string>();
            Debug.Log($"[本地化同步] 成功创建新表格，ID: {newTableId}");
            return newTableId;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[本地化同步] 确保表格存在时发生错误: {ex}");
            throw;
        }
    }

    public async Task PushToFeishu(string tableId = null)
    {
        try
        {
            Debug.Log("[本地化同步] 开始推送数据到飞书...");
            
            foreach (var (tableCollection, tableData) in _allLocalizations)
            {
                Debug.Log($"[本地化同步] 处理表格: {tableCollection.TableCollectionName}");
                
                // 确保表格存在
                tableId = await EnsureTableExists(tableCollection);
                
                // 获取Unity中的所有键
                var unityKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in tableCollection.SharedData.Entries)
                {
                    unityKeys.Add(entry.Key);
                }
                
                // 获取现有记录
                var existingRecords = await _feishuService.GetAllRecords(tableId);
                Debug.Log($"[本地化同步] 从飞书获取到 {existingRecords?.Count ?? 0} 条记录");

                // 检查重复键，特别处理"翻译中"状态的记录
                var keyGroups = existingRecords?
                    .Where(r => r["fields"] != null && r["fields"]["Key"] != null)
                    .GroupBy(r => r["fields"]["Key"].Value<string>(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .ToList();

                // 第一步：检查所有键的状态
                var keysToCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var keysToUpdate = new Dictionary<string, string>();  // key -> recordId
                var keysToDelete = new List<string>();  // recordId list
                var duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 处理重复键
                if (keyGroups != null && keyGroups.Any())
                {
                    foreach (var group in keyGroups)
                    {
                        var translatingRecords = group.Where(r => r["fields"]["Status"]?.Value<string>() == "翻译中").ToList();
                        if (translatingRecords.Any())
                        {
                            var errorMsg = $"发现重复的键 '{group.Key}'，且存在'翻译中'状态的记录：\n";
                            foreach (var record in translatingRecords)
                            {
                                errorMsg += $"记录ID: {record["record_id"]}\n";
                            }
                            Debug.LogError($"[本地化同步] {errorMsg}");
                            throw new Exception(errorMsg);
                        }
                        else
                        {
                            // 对于非"翻译中"状态的重复记录，保留最新的，删除其他的
                            var records = group.OrderByDescending(r => r["last_modified_time"]?.Value<long>() ?? 0).ToList();
                            for (int i = 1; i < records.Count; i++) // 从1开始，跳过第一条（最新的）
                            {
                                keysToDelete.Add(records[i]["record_id"].Value<string>());
                                Debug.Log($"[本地化同步] 标记要删除的重复键记录: {group.Key}, 记录ID: {records[i]["record_id"]}");
                            }
                        }
                    }
                }

                // 处理可能存在的重复键，只保留最后一条记录
                var recordMap = existingRecords != null
                    ? existingRecords.Where(r => r["fields"] != null && r["fields"]["Key"] != null)
                        .GroupBy(r => r["fields"]["Key"].Value<string>(), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            g => g.Key,
                            g => g.OrderByDescending(r => r["last_modified_time"]?.Value<long>() ?? 0).First(),
                            StringComparer.OrdinalIgnoreCase
                        )
                    : new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);

                // 检查需要删除的键
                if (existingRecords != null)
                {
                    foreach (var record in existingRecords)
                    {
                        var fields = record["fields"] as JObject;
                        var key = fields["Key"]?.Value<string>();
                        var status = fields["Status"]?.Value<string>();

                        // 如果key为空或者key不在Unity中，且不是"翻译中"状态，就删除
                        if (string.IsNullOrEmpty(key) || !unityKeys.Contains(key))
                        {
                            if (status == "翻译中")
                            {
                                Debug.Log($"[本地化同步] 键 {(string.IsNullOrEmpty(key) ? "<空>" : key)} 不在Unity中但正在翻译中，保留");
                                continue;
                            }
                            
                            keysToDelete.Add(record["record_id"].Value<string>());
                            Debug.Log($"[本地化同步] 标记要删除的键: {(string.IsNullOrEmpty(key) ? "<空>" : key)}");
                        }
                    }
                }

                // 检查需要创建和更新的键
                foreach (var key in tableData.Keys)
                {
                    if (processedKeys.Contains(key))
                    {
                        Debug.LogWarning($"[本地化同步] 发现重复键: {key}，将被跳过");
                        duplicateKeys.Add(key);
                        continue;
                    }
                    processedKeys.Add(key);

                    if (recordMap.TryGetValue(key, out var existingRecord))
                    {
                        // 检查是否需要更新
                        var existingFields = existingRecord["fields"] as JObject;
                        bool needsUpdate = false;
                        foreach (var locale in LocalizationEditorSettings.GetLocales())
                        {
                            var lang = locale.Identifier.Code;
                            if (tableData[key].TryGetValue(lang, out var newTranslation))
                            {
                                var existingTranslation = existingFields[lang]?.Value<string>();
                                if (existingTranslation != newTranslation)
                                {
                                    needsUpdate = true;
                                    break;
                                }
                            }
                        }

                        if (needsUpdate)
                        {
                            var existingStatus = existingFields["Status"]?.Value<string>();
                            if (existingStatus == "翻译中")
                            {
                                Debug.Log($"[本地化同步] 键 {key} 正在翻译中，跳过同步");
                            }
                            else if (existingStatus == "未完成")
                            {
                                keysToUpdate[key] = existingRecord["record_id"].Value<string>();
                                Debug.Log($"[本地化同步] 标记需要更新的键: {key}");
                            }
                            else
                            {
                                Debug.Log($"[本地化同步] 键 {key} 已完成，跳过更新");
                            }
                        }
                    }
                    else
                    {
                        keysToCreate.Add(key);
                        Debug.Log($"[本地化同步] 标记需要创建的键: {key}");
                    }
                }

                // 第二步：删除不再使用的记录
                if (keysToDelete.Count > 0)
                {
                    Debug.Log($"[本地化同步] 开始删除 {keysToDelete.Count} 条未使用的记录");
                    
                    // 分批删除，每批最多500条
                    int totalBatches = (keysToDelete.Count + 499) / 500; // 向上取整
                    for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                    {
                        var startIndex = batchIndex * 500;
                        var batchSize = Math.Min(500, keysToDelete.Count - startIndex);
                        var batch = keysToDelete.GetRange(startIndex, batchSize);
                        
                        Debug.Log($"[本地化同步] 正在删除第 {batchIndex + 1}/{totalBatches} 批，本批 {batch.Count} 条记录");
                        await _feishuService.BatchDeleteRecords(tableId, batch);
                    }
                    
                    Debug.Log($"[本地化同步] 成功删除所有 {keysToDelete.Count} 条未使用的记录");
                }

                // 第三步：创建新记录
                if (keysToCreate.Count > 0)
                {
                    var createBatch = new List<Dictionary<string, object>>();
                    foreach (var key in keysToCreate)
                    {
                        var fields = new Dictionary<string, object>
                        {
                            { "Key", key },
                            { "Status", "未完成" }
                        };

                        // 添加所有语言的翻译
                        foreach (var locale in LocalizationEditorSettings.GetLocales())
                        {
                            var lang = locale.Identifier.Code;
                            if (tableData[key].TryGetValue(lang, out var translation))
                            {
                                fields[lang] = translation;
                            }
                        }

                        createBatch.Add(fields);
                        if (createBatch.Count >= 500)
                        {
                            await _feishuService.BatchCreateRecords(tableId, createBatch);
                            createBatch.Clear();
                        }
                    }

                    if (createBatch.Count > 0)
                    {
                        await _feishuService.BatchCreateRecords(tableId, createBatch);
                    }

                    Debug.Log($"[本地化同步] 创建了 {keysToCreate.Count} 条新记录");
                }

                // 第四步：更新现有记录
                if (keysToUpdate.Count > 0)
                {
                    var updateBatch = new List<(string recordId, Dictionary<string, object> fields)>();
                    foreach (var (key, recordId) in keysToUpdate)
                    {
                        var fields = new Dictionary<string, object>
                        {
                            { "Key", key }
                        };

                        // 添加所有语言的翻译
                        foreach (var locale in LocalizationEditorSettings.GetLocales())
                        {
                            var lang = locale.Identifier.Code;
                            if (tableData[key].TryGetValue(lang, out var translation))
                            {
                                fields[lang] = translation;
                            }
                        }

                        updateBatch.Add((recordId, fields));
                        if (updateBatch.Count >= 500)
                        {
                            await _feishuService.BatchUpdateRecords(tableId, updateBatch);
                            updateBatch.Clear();
                        }
                    }

                    if (updateBatch.Count > 0)
                    {
                        await _feishuService.BatchUpdateRecords(tableId, updateBatch);
                    }

                    Debug.Log($"[本地化同步] 更新了 {keysToUpdate.Count} 条记录");
                }

                var totalProcessed = keysToCreate.Count + keysToUpdate.Count + keysToDelete.Count;
                Debug.Log($"[本地化同步] 表格 {tableCollection.TableCollectionName} 同步完成");
                Debug.Log($"[本地化同步] 创建: {keysToCreate.Count} 条，更新: {keysToUpdate.Count} 条，删除: {keysToDelete.Count} 条，跳过重复: {duplicateKeys.Count} 条");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[本地化同步] 推送数据时发生错误: {ex}");
            throw;
        }
    }

    public async Task PullFromFeishu(string tableId = null)
    {
        try
        {
            Debug.Log("[本地化同步] 开始从飞书拉取数据...");
            
            // 获取所有本地化表
            var stringTables = LocalizationEditorSettings.GetStringTableCollections();
            if (stringTables.Count == 0)
            {
                Debug.LogWarning("[本地化同步] 未找到本地化表，请先创建本地化表");
                return;
            }

            // 获取所有飞书表格
            var feishuTables = await _feishuService.ListTables();
            var config = FeishuConfig.Instance;
            int totalUpdatedCount = 0;

            foreach (var tableCollection in stringTables)
            {
                Debug.Log($"[本地化同步] 处理表格: {tableCollection.TableCollectionName}");
                
                // 尝试查找对应的飞书表格
                var matchingTable = feishuTables.FirstOrDefault(t => t["name"].Value<string>() == tableCollection.TableCollectionName);
                
                if (matchingTable == null)
                {
                    Debug.LogWarning($"[本地化同步] 未找到对应的飞书表格: {tableCollection.TableCollectionName}，跳过");
                    continue;
                }

                var currentTableId = matchingTable["table_id"].Value<string>();
                var records = await _feishuService.ListRecords(currentTableId);
                int updatedCount = 0;

                // 确保表格数据存在
                if (!_allLocalizations.ContainsKey(tableCollection))
                {
                    _allLocalizations[tableCollection] = new Dictionary<string, Dictionary<string, string>>();
                }
                var tableData = _allLocalizations[tableCollection];

                Debug.Log($"[本地化同步] 获取到 {records.Count} 条记录");
                foreach (var record in records)
                {
                    var fields = record["fields"] as JObject;
                    var key = fields["Key"]?.Value<string>();
                    var status = fields["Status"]?.Value<string>();

                    if (string.IsNullOrEmpty(key))
                    {
                        Debug.LogWarning("[本地化同步] 跳过空键的记录");
                        continue;
                    }

                    if (status == "翻译中")
                    {
                        Debug.Log($"[本地化同步] 跳过翻译中的记录: {key}");
                        continue;
                    }
                    else if (status != "已完成")
                    {
                        Debug.Log($"[本地化同步] 跳过未完成的记录: {key}, 状态: {status}");
                        continue;
                    }

                    if (!tableData.ContainsKey(key))
                    {
                        tableData[key] = new Dictionary<string, string>();
                    }

                    bool hasChanges = false;
                    foreach (var locale in LocalizationEditorSettings.GetLocales())
                    {
                        var lang = locale.Identifier.Code;
                        var translation = fields[lang]?.Value<string>();
                        
                        // 检查本地内容是否有变化，或者是否需要将飞书的空值同步过来
                        if (!tableData[key].ContainsKey(lang) || tableData[key][lang] != translation)
                        {
                            hasChanges = true;
                            tableData[key][lang] = translation; // 即使 translation 是 null 或空，也进行赋值
                            Debug.Log($"[本地化同步] 记录 {key} 更新 {lang} 的翻译 (可能为空值)");
                        }
                    }
                    if (hasChanges)
                    {
                        updatedCount++;
                    }
                }

                Debug.Log($"[本地化同步] 表格 {tableCollection.TableCollectionName} 更新了 {updatedCount} 条记录");
                totalUpdatedCount += updatedCount;
            }

            Debug.Log($"[本地化同步] 总共更新了 {totalUpdatedCount} 条记录");
            SaveToUnityLocalization();
            Debug.Log("[本地化同步] 已保存更改到Unity本地化系统");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[本地化同步] 从飞书拉取数据时发生错误: {ex}");
            throw;
        }
    }

    public async Task SyncWithFeishu(string tableId = null)
    {
        try
        {
            Debug.Log("[本地化同步] 开始双向同步...");
            
            // 获取所有本地化表
            var stringTables = LocalizationEditorSettings.GetStringTableCollections();
            if (stringTables.Count == 0)
            {
                Debug.LogWarning("[本地化同步] 未找到本地化表，请先创建本地化表");
                return;
            }

            foreach (var tableCollection in stringTables)
            {
                Debug.Log($"[本地化同步] 处理表格: {tableCollection.TableCollectionName}");
                
                // 确保表格存在
                var currentTableId = await EnsureTableExists(tableCollection);
                
                // 获取Unity中的所有键
                var unityKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in tableCollection.SharedData.Entries)
                {
                    unityKeys.Add(entry.Key);
                }
                
                // 获取现有记录
                var existingRecords = await _feishuService.GetAllRecords(currentTableId);
                Debug.Log($"[本地化同步] 从飞书获取到 {existingRecords?.Count ?? 0} 条记录");

                // 检查重复键，特别处理"翻译中"状态的记录
                var keyGroups = existingRecords?
                    .Where(r => r["fields"] != null && r["fields"]["Key"] != null)
                    .GroupBy(r => r["fields"]["Key"].Value<string>(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .ToList();
                
                var keysToCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var keysToUpdate = new Dictionary<string, string>();  // key -> recordId
                var keysToDelete = new List<string>();  // recordId list
                var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 处理重复键
                if (keyGroups != null && keyGroups.Any())
                {
                    foreach (var group in keyGroups)
                    {
                        var translatingRecords = group.Where(r => r["fields"]["Status"]?.Value<string>() == "翻译中").ToList();
                        if (translatingRecords.Any())
                {
                            var errorMsg = $"发现重复的键 '{group.Key}'，且存在'翻译中'状态的记录：\n";
                            foreach (var record in translatingRecords)
                            {
                                errorMsg += $"记录ID: {record["record_id"]}\n";
                            }
                            Debug.LogError($"[本地化同步] {errorMsg}");
                            throw new Exception(errorMsg);
                        }
                        else
                        {
                            // 对于非"翻译中"状态的重复记录，保留最新的，删除其他的
                            var records = group.OrderByDescending(r => r["last_modified_time"]?.Value<long>() ?? 0).ToList();
                            for (int i = 1; i < records.Count; i++) // 从1开始，跳过第一条（最新的）
                            {
                                keysToDelete.Add(records[i]["record_id"].Value<string>());
                                Debug.Log($"[本地化同步] 标记要删除的重复键记录: {group.Key}, 记录ID: {records[i]["record_id"]}");
                }
                        }
                    }
                }

                // 处理可能存在的重复键，只保留最后一条记录
                var recordMap = existingRecords != null
                    ? existingRecords.Where(r => r["fields"] != null && r["fields"]["Key"] != null)
                        .GroupBy(r => r["fields"]["Key"].Value<string>(), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            g => g.Key,
                            g => g.OrderByDescending(r => r["last_modified_time"]?.Value<long>() ?? 0).First(),
                            StringComparer.OrdinalIgnoreCase
                        )
                    : new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);

                // 检查需要删除的键
                if (existingRecords != null)
                {
                    foreach (var record in existingRecords)
                    {
                        var fields = record["fields"] as JObject;
                        var key = fields["Key"]?.Value<string>();
                        var status = fields["Status"]?.Value<string>();

                        // 如果key为空或者key不在Unity中，且不是"翻译中"状态，就删除
                        if (string.IsNullOrEmpty(key) || !unityKeys.Contains(key))
                        {
                            if (status == "翻译中")
                            {
                                Debug.Log($"[本地化同步] 键 {(string.IsNullOrEmpty(key) ? "<空>" : key)} 不在Unity中但正在翻译中，保留");
                                continue;
                            }
                            
                            keysToDelete.Add(record["record_id"].Value<string>());
                            Debug.Log($"[本地化同步] 标记要删除的键: {(string.IsNullOrEmpty(key) ? "<空>" : key)}");
                        }
                    }
                }

                // 确保表格数据存在
                if (!_allLocalizations.ContainsKey(tableCollection))
                {
                    _allLocalizations[tableCollection] = new Dictionary<string, Dictionary<string, string>>();
                }
                var tableData = _allLocalizations[tableCollection];

                // 检查需要创建和更新的键
                foreach (var key in tableData.Keys)
                    {
                    if (processedKeys.Contains(key))
                    {
                        Debug.LogWarning($"[本地化同步] 发现重复键: {key}，将被跳过");
                        continue;
                    }
                    processedKeys.Add(key);

                    if (recordMap.TryGetValue(key, out var existingRecord))
                    {
                        var existingFields = existingRecord["fields"] as JObject;
                        var status = existingFields["Status"]?.Value<string>();
                        
                        if (status == "翻译中")
                        {
                            Debug.Log($"[本地化同步] 跳过翻译中的记录: {key}");
                            continue;
                        }
                        else if (status == "未完成")
                        {
                            // 检查是否需要更新
                            bool needsUpdate = false;
                            foreach (var locale in LocalizationEditorSettings.GetLocales())
                            {
                                var lang = locale.Identifier.Code;
                                if (tableData[key].TryGetValue(lang, out var newTranslation))
                                {
                                    var existingTranslation = existingFields[lang]?.Value<string>();
                                    if (existingTranslation != newTranslation)
                                    {
                                        needsUpdate = true;
                                        break;
                                    }
                                }
                            }

                            if (needsUpdate)
                            {
                                keysToUpdate[key] = existingRecord["record_id"].Value<string>();
                                Debug.Log($"[本地化同步] 标记需要更新的键: {key}");
                            }
                        }
                        else
                        {
                            Debug.Log($"[本地化同步] 跳过已完成的记录: {key}");
                        }
                    }
                    else
                    {
                        keysToCreate.Add(key);
                        Debug.Log($"[本地化同步] 标记需要创建的键: {key}");
                    }
                    }

                // 删除不再使用的记录
                if (keysToDelete.Count > 0)
                {
                    Debug.Log($"[本地化同步] 开始删除 {keysToDelete.Count} 条未使用的记录");
                    
                    // 分批删除，每批最多500条
                    int totalBatches = (keysToDelete.Count + 499) / 500;
                    for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                    {
                        var startIndex = batchIndex * 500;
                        var batchSize = Math.Min(500, keysToDelete.Count - startIndex);
                        var batch = keysToDelete.GetRange(startIndex, batchSize);
                        
                        Debug.Log($"[本地化同步] 正在删除第 {batchIndex + 1}/{totalBatches} 批，本批 {batch.Count} 条记录");
                        await _feishuService.BatchDeleteRecords(currentTableId, batch);
                    }
                    
                    Debug.Log($"[本地化同步] 成功删除所有 {keysToDelete.Count} 条未使用的记录");
                }

                // 创建新记录
                if (keysToCreate.Count > 0)
                {
                    var createBatch = new List<Dictionary<string, object>>();
                    foreach (var key in keysToCreate)
                    {
                        var fields = new Dictionary<string, object>
                        {
                            { "Key", key },
                            { "Status", "未完成" }
                        };

                        // 添加所有语言的翻译
                        foreach (var locale in LocalizationEditorSettings.GetLocales())
                        {
                            var lang = locale.Identifier.Code;
                            if (tableData[key].TryGetValue(lang, out var translation))
                            {
                                fields[lang] = translation;
                    }
                        }

                        createBatch.Add(fields);
                        if (createBatch.Count >= 500)
                    {
                            await _feishuService.BatchCreateRecords(currentTableId, createBatch);
                            createBatch.Clear();
                    }
                }

                    if (createBatch.Count > 0)
                {
                        await _feishuService.BatchCreateRecords(currentTableId, createBatch);
                    }

                    Debug.Log($"[本地化同步] 创建了 {keysToCreate.Count} 条新记录");
                }

                // 更新现有记录
                if (keysToUpdate.Count > 0)
                {
                    var updateBatch = new List<(string recordId, Dictionary<string, object> fields)>();
                    foreach (var (key, recordId) in keysToUpdate)
                    {
                        var fields = new Dictionary<string, object>
                        {
                            { "Key", key }
                        };

                        // 添加所有语言的翻译
                        foreach (var locale in LocalizationEditorSettings.GetLocales())
                        {
                            var lang = locale.Identifier.Code;
                            if (tableData[key].TryGetValue(lang, out var translation))
                            {
                                fields[lang] = translation;
                            }
                        }

                        updateBatch.Add((recordId, fields));
                        if (updateBatch.Count >= 500)
                        {
                            await _feishuService.BatchUpdateRecords(currentTableId, updateBatch);
                            updateBatch.Clear();
                }
                    }

                    if (updateBatch.Count > 0)
                {
                        await _feishuService.BatchUpdateRecords(currentTableId, updateBatch);
                    }

                    Debug.Log($"[本地化同步] 更新了 {keysToUpdate.Count} 条记录");
                }

                var totalProcessed = keysToCreate.Count + keysToUpdate.Count + keysToDelete.Count;
                Debug.Log($"[本地化同步] 表格 {tableCollection.TableCollectionName} 同步完成");
                Debug.Log($"[本地化同步] 创建: {keysToCreate.Count} 条，更新: {keysToUpdate.Count} 条，删除: {keysToDelete.Count} 条");
            }

            // 从飞书拉取已完成的翻译
            Debug.Log("[本地化同步] 开始拉取已完成的翻译...");
            await PullFromFeishu();
            
            Debug.Log("[本地化同步] 双向同步完成");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[本地化同步] 双向同步时发生错误: {ex}");
            throw;
        }
    }

    private void SaveToUnityLocalization()
    {
        try
        {
            Debug.Log("[本地化同步] 开始保存到Unity本地化系统...");
            
            foreach (var (tableCollection, tableData) in _allLocalizations)
            {
                Debug.Log($"[本地化同步] 处理表格: {tableCollection.TableCollectionName}");
                int totalUpdated = 0;

                // 遍历所有语言
                foreach (var locale in LocalizationEditorSettings.GetLocales())
                {
                    Debug.Log($"[本地化同步] 处理语言: {locale.Identifier.Code}");
                    var table = tableCollection.GetTable(locale.Identifier) as StringTable;
                    if (table == null)
                    {
                        Debug.LogWarning($"[本地化同步] 未找到语言 {locale.Identifier.Code} 的字符串表");
                        continue;
                    }

                    int langUpdated = 0;
                    // 遍历所有本地化键
                    foreach (var (key, translations) in tableData)
                    {
                        if (!translations.ContainsKey(locale.Identifier.Code))
                            continue;

                        var value = translations[locale.Identifier.Code];
                        var entry = table.GetEntry(key);
                        
                        if (entry == null)
                        {
                            // 添加新条目
                            var sharedEntry = tableCollection.SharedData.GetEntry(key);
                            if (sharedEntry == null)
                            {
                                sharedEntry = tableCollection.SharedData.AddKey(key);
                                Debug.Log($"[本地化同步] 添加新键: {key}");
                            }
                            entry = table.AddEntry(sharedEntry.Id, value);
                        }
                        else
                        {
                            // 更新现有条目
                            entry.Value = value;
                        }
                        langUpdated++;
                    }

                    Debug.Log($"[本地化同步] 语言 {locale.Identifier.Code} 更新了 {langUpdated} 个条目");
                    totalUpdated += langUpdated;
                    EditorUtility.SetDirty(table);
                }

                Debug.Log($"[本地化同步] 表格 {tableCollection.TableCollectionName} 更新了 {totalUpdated} 个条目");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[本地化同步] 保存完成");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[本地化同步] 保存到Unity本地化系统时发生错误: {ex}");
            throw;
        }
    }

    public async Task CleanUnusedKeys()
    {
        try
        {
            Debug.Log("[本地化同步] 开始清理未使用的键...");
            
            // 获取所有本地化表
            var stringTables = LocalizationEditorSettings.GetStringTableCollections();
            if (stringTables.Count == 0)
            {
                Debug.LogWarning("[本地化同步] 未找到本地化表，请先创建本地化表");
                return;
            }

            // 获取所有飞书表格
            var feishuTables = await _feishuService.ListTables();
            int totalRemovedCount = 0;

            foreach (var tableCollection in stringTables)
            {
                Debug.Log($"[本地化同步] 处理表格: {tableCollection.TableCollectionName}");
                
                // 获取Unity中的所有键
                var unityKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in tableCollection.SharedData.Entries)
                {
                    unityKeys.Add(entry.Key);
                }
                
                // 尝试查找对应的飞书表格
                var matchingTable = feishuTables.FirstOrDefault(t => t["name"].Value<string>() == tableCollection.TableCollectionName);
                if (matchingTable == null)
                {
                    Debug.LogWarning($"[本地化同步] 未找到对应的飞书表格: {tableCollection.TableCollectionName}，跳过");
                    continue;
                }

                var currentTableId = matchingTable["table_id"].Value<string>();
                var records = await _feishuService.ListRecords(currentTableId);
                var recordsToDelete = new List<string>();

                Debug.Log($"[本地化同步] 检查 {records.Count} 条飞书记录");
                foreach (var record in records)
                {
                    var fields = record["fields"] as JObject;
                    var key = fields["Key"]?.Value<string>();
                    var status = fields["Status"]?.Value<string>();

                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }

                    // 如果键不在Unity中，且不是"翻译中"状态，则标记为删除
                    if (!unityKeys.Contains(key))
                    {
                        if (status == "翻译中")
                        {
                            Debug.Log($"[本地化同步] 键 {key} 不在Unity中但正在翻译中，保留");
                            continue;
                        }
                        
                        recordsToDelete.Add(record["record_id"].Value<string>());
                        Debug.Log($"[本地化同步] 标记要删除的键: {key}");
                    }
                }

                // 批量删除记录
                if (recordsToDelete.Count > 0)
                {
                    Debug.Log($"[本地化同步] 删除 {recordsToDelete.Count} 条未使用的记录");
                    
                    // 分批删除，每批最多500条
                    for (int i = 0; i < recordsToDelete.Count; i += 500)
                    {
                        var batch = recordsToDelete.Skip(i).Take(500).ToList();
                        await _feishuService.BatchDeleteRecords(currentTableId, batch);
                    }
                    
                    totalRemovedCount += recordsToDelete.Count;
                }

                Debug.Log($"[本地化同步] 表格 {tableCollection.TableCollectionName} 清理完成");
            }

            Debug.Log($"[本地化同步] 清理完成，共删除 {totalRemovedCount} 条未使用的记录");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[本地化同步] 清理未使用的键时发生错误: {ex}");
            throw;
        }
    }

    public async Task UpdateReviewForTable(StringTableCollection tableCollection, Dictionary<string, string> reviewByKey, Dictionary<string, string> statusByKey)
    {
        try
        {
            var tableId = await EnsureTableExists(tableCollection);
            var existingRecords = await _feishuService.GetAllRecords(tableId);
            var recordMap = existingRecords != null
                ? existingRecords.Where(r => r["fields"] != null && r["fields"]["Key"] != null)
                    .GroupBy(r => r["fields"]["Key"].Value<string>(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(r => r["last_modified_time"]?.Value<long>() ?? 0).First(),
                        StringComparer.OrdinalIgnoreCase
                    )
                : new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);

            var updateBatch = new List<(string recordId, Dictionary<string, object> fields)>();
            var createBatch = new List<Dictionary<string, object>>();

            foreach (var (key, reviewText) in reviewByKey)
            {
                if (string.IsNullOrEmpty(key)) continue;
                var value = string.IsNullOrEmpty(reviewText) ? "" : reviewText;
                if (recordMap.TryGetValue(key, out var record))
                {
                    var fields = new Dictionary<string, object>
                    {
                        { "Key", key }
                    };
                    int failCount = 0;
                    if (!string.IsNullOrEmpty(value))
                    {
                        var lines = value.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            var idx = line.IndexOf(':');
                            if (idx > 0)
                            {
                                var lang = line.Substring(0, idx).Trim();
                                var text = line.Substring(idx + 1).Trim();
                                bool pass = string.Equals(text, "OK", StringComparison.OrdinalIgnoreCase) || text.EndsWith("OK") || text.Contains(": OK");
                                fields[$"Review_{lang}"] = string.IsNullOrEmpty(text) ? "OK" : text;
                                if (!pass) failCount++;
                            }
                        }
                    }
                    fields["ReviewStatus"] = failCount == 0 ? "通过" : $"未通过({failCount})";
                    updateBatch.Add((record["record_id"].Value<string>(), fields));
                    if (updateBatch.Count >= 500)
                    {
                        await _feishuService.BatchUpdateRecords(tableId, updateBatch);
                        updateBatch.Clear();
                    }
                }
                else
                {
                    var fields = new Dictionary<string, object>
                    {
                        { "Key", key },
                        { "Status", "未完成" }
                    };
                    int failCount = 0;
                    if (!string.IsNullOrEmpty(value))
                    {
                        var lines = value.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            var idx = line.IndexOf(':');
                            if (idx > 0)
                            {
                                var lang = line.Substring(0, idx).Trim();
                                var text = line.Substring(idx + 1).Trim();
                                bool pass = string.Equals(text, "OK", StringComparison.OrdinalIgnoreCase) || text.EndsWith("OK") || text.Contains(": OK");
                                fields[$"Review_{lang}"] = string.IsNullOrEmpty(text) ? "OK" : text;
                                if (!pass) failCount++;
                            }
                        }
                    }
                    fields["ReviewStatus"] = failCount == 0 ? "通过" : $"未通过({failCount})";
                    createBatch.Add(fields);
                    if (createBatch.Count >= 500)
                    {
                        await _feishuService.BatchCreateRecords(tableId, createBatch);
                        createBatch.Clear();
                    }
                }
            }

            if (updateBatch.Count > 0)
            {
                await _feishuService.BatchUpdateRecords(tableId, updateBatch);
            }
            if (createBatch.Count > 0)
            {
                await _feishuService.BatchCreateRecords(tableId, createBatch);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[本地化同步] 更新审阅结果时发生错误: {ex}");
            throw;
        }
    }

    public async Task UpdateTranslationsForTable(StringTableCollection tableCollection, Dictionary<string, Dictionary<string, string>> translationsByKey)
    {
        try
        {
            Debug.Log($"[本地化同步] 更新翻译列开始，待处理键数: {translationsByKey?.Count ?? 0}");
            var tableId = await EnsureTableExists(tableCollection);
            var existingRecords = await _feishuService.GetAllRecords(tableId);
            var recordMap = existingRecords != null
                ? existingRecords.Where(r => r["fields"] != null && r["fields"]["Key"] != null)
                    .GroupBy(r => r["fields"]["Key"].Value<string>(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(r => r["last_modified_time"]?.Value<long>() ?? 0).First(),
                        StringComparer.OrdinalIgnoreCase
                    )
                : new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
            var updateBatch = new List<(string recordId, Dictionary<string, object> fields)>();
            var createBatch = new List<Dictionary<string, object>>();
            int updates = 0;
            int creates = 0;
            foreach (var (key, langMap) in translationsByKey)
            {
                if (string.IsNullOrEmpty(key) || langMap == null || langMap.Count == 0) continue;
                if (recordMap.TryGetValue(key, out var record))
                {
                    var fields = new Dictionary<string, object> { { "Key", key } };
                    foreach (var kv in langMap)
                    {
                        if (!string.IsNullOrEmpty(kv.Key) && kv.Value != null)
                        {
                            fields[kv.Key] = kv.Value;
                        }
                    }
                    updateBatch.Add((record["record_id"].Value<string>(), fields));
                    updates++;
                    if (updateBatch.Count >= 500)
                    {
                        await _feishuService.BatchUpdateRecords(tableId, updateBatch);
                        updateBatch.Clear();
                    }
                }
                else
                {
                    var fields = new Dictionary<string, object>
                    {
                        { "Key", key },
                        { "Status", "未完成" }
                    };
                    foreach (var kv in langMap)
                    {
                        if (!string.IsNullOrEmpty(kv.Key) && kv.Value != null)
                        {
                            fields[kv.Key] = kv.Value;
                        }
                    }
                    createBatch.Add(fields);
                    creates++;
                    if (createBatch.Count >= 500)
                    {
                        await _feishuService.BatchCreateRecords(tableId, createBatch);
                        createBatch.Clear();
                    }
                }
            }
            if (updateBatch.Count > 0)
            {
                await _feishuService.BatchUpdateRecords(tableId, updateBatch);
            }
            if (createBatch.Count > 0)
            {
                await _feishuService.BatchCreateRecords(tableId, createBatch);
            }
            Debug.Log($"[本地化同步] 更新翻译列完成，更新: {updates}，创建: {creates}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[本地化同步] 更新翻译列时发生错误: {ex}");
            throw;
        }
    }
}