using UnityEngine;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;

public class FeishuService
{
    private const string BASE_URL = "https://open.feishu.cn/open-apis/";
    private static readonly HttpClient _httpClient = new HttpClient();

    public async Task<string> GetAccessToken()
    {
        try
        {
            Debug.Log("[飞书API] 开始获取访问令牌...");
            
            var config = FeishuConfig.Instance;
            if (string.IsNullOrEmpty(config.AppId) || string.IsNullOrEmpty(config.AppSecret))
            {
                throw new Exception("AppId 或 AppSecret 未配置");
            }

            var requestBody = new
            {
                app_id = config.AppId,
                app_secret = config.AppSecret
            };

            Debug.Log($"[飞书API] 请求新的访问令牌，AppId: {config.AppId}");
            var response = await PostAsync("auth/v3/tenant_access_token/internal/", requestBody, false);
            var result = JObject.Parse(response);

            if (result["code"].Value<int>() == 0)
            {
                var token = result["tenant_access_token"].Value<string>();
                Debug.Log("[飞书API] 成功获取新的访问令牌");
                return token;
            }

            var errorMsg = $"获取访问令牌失败: {result["msg"]}, 错误码: {result["code"]}";
            Debug.LogError($"[飞书API] {errorMsg}");
            throw new Exception(errorMsg);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[飞书API] 获取访问令牌时发生异常: {ex}");
            throw;
        }
    }

    public async Task<JObject> GetBitableMetadata()
    {
        var config = FeishuConfig.Instance;
        var response = await GetAsync($"bitable/v1/apps/{config.TableId}");
        return JObject.Parse(response);
    }

    public async Task<JArray> ListTables()
    {
        try
        {
            Debug.Log("[飞书API] 开始获取表格列表...");
            var config = FeishuConfig.Instance;
            
            if (string.IsNullOrEmpty(config.TableId))
            {
                throw new Exception("TableId 未配置");
            }

            var response = await GetAsync($"bitable/v1/apps/{config.TableId}/tables");
            var result = JObject.Parse(response);

            if (result["code"].Value<int>() != 0)
            {
                var errorMsg = $"获取表格列表失败: {result["msg"]}, 错误码: {result["code"]}";
                Debug.LogError($"[飞书API] {errorMsg}");
                throw new Exception(errorMsg);
            }

            var tables = result["data"]["items"] as JArray;
            Debug.Log($"[飞书API] 成功获取表格列表，共 {tables?.Count ?? 0} 个表格");
            return tables;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[飞书API] 获取表格列表时发生异常: {ex}");
            throw;
        }
    }

    private async Task<string> GetAsync(string endpoint)
    {
        try
        {
            Debug.Log($"[飞书API] 发起GET请求: {endpoint}");
            var token = await GetAccessToken();
            var request = new HttpRequestMessage(HttpMethod.Get, BASE_URL + endpoint);
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"[飞书API] HTTP请求失败: {response.StatusCode}, 响应内容: {content}");
                throw new HttpRequestException($"HTTP请求失败: {response.StatusCode}");
            }

            Debug.Log($"[飞书API] GET请求成功: {endpoint}");
            return content;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[飞书API] GET请求异常: {endpoint}, 错误: {ex}");
            throw;
        }
    }

    private async Task<string> PostAsync(string endpoint, object data, bool needToken = true)
    {
        try
        {
            Debug.Log($"[飞书API] 发起POST请求: {endpoint}");
            var jsonData = JsonConvert.SerializeObject(data);
            Debug.Log($"[飞书API] 请求数据: {jsonData}");

            var request = new HttpRequestMessage(HttpMethod.Post, BASE_URL + endpoint);
            
            if (needToken)
            {
                var token = await GetAccessToken();
                request.Headers.Add("Authorization", $"Bearer {token}");
            }

            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"[飞书API] HTTP请求失败: {response.StatusCode}, 响应内容: {responseContent}");
                throw new HttpRequestException($"HTTP请求失败: {response.StatusCode}");
            }

            Debug.Log($"[飞书API] POST请求成功: {endpoint}");
            Debug.Log($"[飞书API] 响应内容: {responseContent}");
            return responseContent;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[飞书API] POST请求异常: {endpoint}, 错误: {ex}");
            throw;
        }
    }

    public async Task<JObject> CreateTable(string tableName)
    {
        try
        {
            Debug.Log($"[飞书API] 开始创建表格: {tableName}");
            var config = FeishuConfig.Instance;
            
            // 获取所有支持的语言
            var locales = LocalizationEditorSettings.GetLocales();
            var languageCodes = locales.Select(l => l.Identifier.Code).ToList();
            
            var requestBody = new
            {
                table = new
                {
                    name = tableName,
                    default_view_name = "本地化数据",
                    fields = new object[]
                    {
                        new
                        {
                            field_name = "Key",
                            type = 1
                        },
                        new
                        {
                            field_name = "Description",
                            type = 1
                        },
                        new
                        {
                            field_name = "Status",
                            type = 3,
                            property = new
                            {
                                options = new object[]
                                {
                                    new { name = "未完成" },
                                    new { name = "翻译中" },
                                    new { name = "已完成" }
                                }
                            }
                        }
                    }.Concat(languageCodes.Select(lang => new
                    {
                        field_name = lang,
                        type = 1
                    })).ToArray()
                }
            };

            Debug.Log($"[飞书API] 创建表格请求数据: {JsonConvert.SerializeObject(requestBody, Formatting.Indented)}");

            var token = await GetAccessToken();
            var content = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}bitable/v1/apps/{config.TableId}/tables");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            Debug.Log($"[飞书API] 创建表格响应: {responseContent}");
            
            var result = JObject.Parse(responseContent);
            if (result["code"].Value<int>() != 0)
            {
                var error = $"创建表格失败: {result["msg"]}, 错误码: {result["code"]}";
                Debug.LogError($"[飞书API] {error}");
                throw new Exception(error);
            }

            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[飞书API] 创建表格时发生异常: {ex}");
            throw;
        }
    }

    public async Task<JObject> UpdateRecord(string tableId, string recordId, Dictionary<string, object> fields)
    {
        var config = FeishuConfig.Instance;
        var requestBody = new
        {
            fields = fields
        };

        var token = await GetAccessToken();
        var content = new StringContent(
            JsonConvert.SerializeObject(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var request = new HttpRequestMessage(HttpMethod.Put, $"{BASE_URL}bitable/v1/apps/{config.TableId}/tables/{tableId}/records/{recordId}");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        return JObject.Parse(responseContent);
    }

    public async Task<JObject> CreateRecord(string tableId, Dictionary<string, object> fields)
    {
        var config = FeishuConfig.Instance;
        var requestBody = new
        {
            fields = fields
        };

        var token = await GetAccessToken();
        var content = new StringContent(
            JsonConvert.SerializeObject(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}bitable/v1/apps/{config.TableId}/tables/{tableId}/records");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        return JObject.Parse(responseContent);
    }

    public async Task<JArray> ListRecords(string tableId, string viewId = null, string filter = null)
    {
        var config = FeishuConfig.Instance;
        var url = $"bitable/v1/apps/{config.TableId}/tables/{tableId}/records?page_size=500";
        
        if (!string.IsNullOrEmpty(viewId))
        {
            url += $"&view_id={viewId}";
        }
        
        if (!string.IsNullOrEmpty(filter))
        {
            url += $"&filter={Uri.EscapeDataString(filter)}";
        }

        var response = await GetAsync(url);
        var result = JObject.Parse(response);
        
        if (result["code"].Value<int>() != 0)
        {
            throw new Exception($"获取记录失败: {result["msg"]}");
        }

        return result["data"]["items"] as JArray;
    }

    public async Task<JObject> BatchCreateRecords(string tableId, List<Dictionary<string, object>> recordsList)
    {
        var config = FeishuConfig.Instance;
        var requestBody = new
        {
            records = recordsList.Select(fields => new { fields }).ToList()
        };

        var token = await GetAccessToken();
        var content = new StringContent(
            JsonConvert.SerializeObject(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}bitable/v1/apps/{config.TableId}/tables/{tableId}/records/batch_create");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JObject.Parse(responseContent);
        if (result["code"].Value<int>() != 0)
        {
            throw new Exception($"批量创建失败: {result["msg"]}");
        }
        return result;
    }

    public async Task<JObject> BatchUpdateRecords(string tableId, List<(string recordId, Dictionary<string, object> fields)> updates)
    {
        var config = FeishuConfig.Instance;
        var requestBody = new
        {
            records = updates.Select(update => new
            {
                record_id = update.recordId,
                fields = update.fields
            }).ToList()
        };

        var token = await GetAccessToken();
        var content = new StringContent(
            JsonConvert.SerializeObject(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}bitable/v1/apps/{config.TableId}/tables/{tableId}/records/batch_update");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JObject.Parse(responseContent);
        if (result["code"].Value<int>() != 0)
        {
            throw new Exception($"批量更新失败: {result["msg"]}");
        }
        return result;
    }

    public async Task<JObject> DeleteRecord(string tableId, string recordId)
    {
        var config = FeishuConfig.Instance;
        var token = await GetAccessToken();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BASE_URL}bitable/v1/apps/{config.TableId}/tables/{tableId}/records/{recordId}");
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        return JObject.Parse(responseContent);
    }

    public async Task<JObject> BatchDeleteRecords(string tableId, List<string> recordIds)
    {
        try
        {
            var config = FeishuConfig.Instance;
            var url = $"bitable/v1/apps/{config.TableId}/tables/{tableId}/records/batch_delete";
            var data = new { records = recordIds };
            var response = await PostAsync(url, data);
            var result = JObject.Parse(response);

            if (result["code"].Value<int>() != 0)
            {
                throw new System.Exception($"删除记录失败: {result["msg"]}");
            }

            var deletedCount = result["data"]["records"].Count();
            var failedCount = recordIds.Count - deletedCount;
            
            if (failedCount > 0)
            {
                Debug.LogWarning($"[飞书服务] {failedCount} 条记录删除失败");
            }
            
            Debug.Log($"[飞书服务] 成功删除 {deletedCount} 条记录");
            return result;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[飞书服务] 删除记录时发生错误: {ex}");
            throw;
        }
    }

    public async Task<JArray> GetAllRecords(string tableId, string viewId = null, string filter = null)
    {
        var allRecords = new List<JToken>();
        string pageToken = null;
        
        do
        {
            var url = $"bitable/v1/apps/{FeishuConfig.Instance.TableId}/tables/{tableId}/records?page_size=500";
            if (!string.IsNullOrEmpty(pageToken))
            {
                url += $"&page_token={pageToken}";
            }
            if (!string.IsNullOrEmpty(viewId))
            {
                url += $"&view_id={viewId}";
            }
            if (!string.IsNullOrEmpty(filter))
            {
                url += $"&filter={Uri.EscapeDataString(filter)}";
            }

            var response = await GetAsync(url);
            var result = JObject.Parse(response);

            if (result["code"].Value<int>() != 0)
            {
                throw new Exception($"获取记录失败: {result["msg"]}");
            }

            var items = result["data"]["items"] as JArray;
            if (items != null && items.Count > 0)
            {
                allRecords.AddRange(items);
                Debug.Log($"[飞书API] 已获取 {allRecords.Count} 条记录");
            }

            pageToken = result["data"]["page_token"]?.Value<string>();
        } while (!string.IsNullOrEmpty(pageToken));

        Debug.Log($"[飞书API] 获取记录完成，共 {allRecords.Count} 条记录");
        return new JArray(allRecords);
    }

    public async Task<JObject> GetTableFields(string tableId)
    {
        var config = FeishuConfig.Instance;
        var response = await GetAsync($"bitable/v1/apps/{config.TableId}/tables/{tableId}/fields");
        return JObject.Parse(response);
    }

    public async Task<JObject> CreateField(string tableId, string fieldName, int fieldType)
    {
        var config = FeishuConfig.Instance;
        var requestBody = new
        {
            field_name = fieldName,
            type = fieldType
        };

        var response = await PostAsync($"bitable/v1/apps/{config.TableId}/tables/{tableId}/fields", requestBody);
        return JObject.Parse(response);
    }

    public async Task EnsureFieldsExist(string tableId, List<(string name, int type)> requiredFields)
    {
        var existingFields = await GetTableFields(tableId);
        var currentFields = existingFields["data"]["items"] as JArray;
        var currentFieldNames = currentFields.Select(f => f["field_name"].Value<string>()).ToList();

        foreach (var (fieldName, fieldType) in requiredFields)
        {
            if (!currentFieldNames.Contains(fieldName))
            {
                Debug.Log($"[飞书API] 创建字段: {fieldName}");
                await CreateField(tableId, fieldName, fieldType);
            }
        }
    }

    public async Task<JArray> SearchRecords(string tableId, string filter = null)
    {
        var config = FeishuConfig.Instance;
        var requestBody = new
        {
            page_size = 500,
            filter = filter != null ? new
            {
                conditions = new[]
                {
                    new
                    {
                        field_name = "Key",
                        @operator = "is",
                        value = new[] { filter }
                    }
                }
            } : null
        };

        var token = await GetAccessToken();
        var content = new StringContent(
            JsonConvert.SerializeObject(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}bitable/v1/apps/{config.TableId}/tables/{tableId}/records/search");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JObject.Parse(responseContent);

        if (result["code"].Value<int>() != 0)
        {
            throw new Exception($"搜索记录失败: {result["msg"]}");
        }

        return result["data"]["items"] as JArray;
    }

    public async Task<JObject> BatchGetRecords(string tableId, List<string> recordIds)
    {
        var config = FeishuConfig.Instance;
        var requestBody = new
        {
            record_ids = recordIds
        };

        var token = await GetAccessToken();
        var content = new StringContent(
            JsonConvert.SerializeObject(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}bitable/v1/apps/{config.TableId}/tables/{tableId}/records/batch_get");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        return JObject.Parse(responseContent);
    }
}