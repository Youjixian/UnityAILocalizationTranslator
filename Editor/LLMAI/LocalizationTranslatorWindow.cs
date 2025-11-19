using UnityEngine;
using UnityEditor;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine.Localization.Settings;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using Newtonsoft.Json.Linq;

namespace CardGame.Editor.LLMAI
{
    public class LocalizationTranslatorWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private StringTableCollection selectedCollection;
        private bool isTranslating = false;
        private float translationProgress = 0f;
        private string statusMessage = "";
        private bool showAdvancedOptions = false;
        private bool translateEmptyOnly = true;
        private bool translateWithFeishuDescription = false;
        private bool includeDescriptionForTranslation = false;
        private bool includeLocalizationKey = true;
        private string sourceLanguage = "zh-CN";
        private List<string> selectedLanguages = new List<string>();
        private Dictionary<string, bool> languageToggles = new Dictionary<string, bool>();
        private FeishuService _feishuService;
        private Dictionary<string, string> _keyDescriptions;
        private int completedTranslations = 0;
        private int totalTranslations = 0;
        private CancellationTokenSource _cancellationTokenSource;
        private bool hasShownResult = false;
        private bool isPaused = false;
        private TaskCompletionSource<bool> pauseCompletionSource;

        private bool isReviewing = false;
        private float reviewProgress = 0f;
        private string reviewStatusMessage = "";
        private int completedReviews = 0;
        private int totalReviews = 0;
        private bool reviewOnlyNonEmpty = false;
        private bool reviewOnlyEmptyReview = false;
        private bool reviewOnlyDescribed = false;
        private bool outputToFeishuReview = true;
        private bool outputToFeishuReviewIncremental = true;
        private string reviewConstraints = "";
        private Dictionary<string, List<string>> _reviewIssuesByKey = new Dictionary<string, List<string>>();
        private Dictionary<string, int> _lastFlushedCounts = new Dictionary<string, int>();
        private GUIStyle reviewTextAreaStyle;
        private bool translateOnlyFailed = false;
        private bool translateWithReviewComments = false;

        [MenuItem("Tools/Localization/AI Translator")]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalizationTranslatorWindow>(I18N.T("WindowTitleLLMAI"));
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            // 初始化飞书服务
            _feishuService = new FeishuService();
            _keyDescriptions = new Dictionary<string, string>();
            
            // 初始化语言选择
            var locales = LocalizationEditorSettings.GetLocales();
            languageToggles.Clear();
            foreach (var locale in locales)
            {
                if (locale.Identifier.Code != sourceLanguage)
                {
                    languageToggles[locale.Identifier.Code] = true;
                }
            }
        }

        private void OnDisable()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private void OnGUI()
        {
            GUILayout.Label(I18N.T("WindowTitleLLMAI"), EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(I18N.T("WindowTitleLLMAI"), EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (GUILayout.Button(I18N.T("OpenLLMSettings")))
            {
                LLMAIConfigWindow.ShowWindow();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(I18N.T("BasicSettings"), EditorStyles.boldLabel);
            
            // 选择字符串表
            var collections = LocalizationEditorSettings.GetStringTableCollections().ToArray();
            int selectedIndex = -1;
            if (selectedCollection != null)
            {
                selectedIndex = System.Array.FindIndex(collections, c => c == selectedCollection);
            }
            
            int newIndex = EditorGUILayout.Popup(I18N.T("SelectStringTable"), selectedIndex, 
                collections.Select(c => c.TableCollectionName).ToArray());
            
            if (newIndex != selectedIndex)
            {
                selectedCollection = collections[newIndex];
            }

            // 添加翻译选项
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(I18N.T("TranslationOptions"), EditorStyles.boldLabel);

            // 是否携带key一同翻译（所有表都可用）
            var keyContent = new GUIContent(
                I18N.T("IncludeLocalizationKey"),
                I18N.T("KeyTooltip")
            );
            includeLocalizationKey = EditorGUILayout.Toggle(keyContent, includeLocalizationKey);

            // 源语言选择
            var locales = LocalizationEditorSettings.GetLocales();
            var localeNames = locales.Select(l => $"{l.LocaleName} ({l.Identifier.Code})").ToArray();
            var localeIds = locales.Select(l => l.Identifier.Code).ToArray();
            
            int sourceIndex = System.Array.IndexOf(localeIds, sourceLanguage);
            int newSourceIndex = EditorGUILayout.Popup(I18N.T("SourceLanguage"), sourceIndex, localeNames);
            if (newSourceIndex != sourceIndex)
            {
                sourceLanguage = localeIds[newSourceIndex];
                // 更新语言选择列表
                languageToggles.Clear();
                foreach (var locale in locales)
                {
                    if (locale.Identifier.Code != sourceLanguage)
                    {
                        languageToggles[locale.Identifier.Code] = true;
                    }
                }
            }

            translateEmptyOnly = EditorGUILayout.Toggle(
                new GUIContent(I18N.T("TranslateOnlyEmpty"), I18N.T("EmptyTooltip")),
                translateEmptyOnly
            );

            // 添加携带描述选项
            var includeDescContent = new GUIContent(
                I18N.T("IncludeDescription"),
                I18N.T("DescriptionTooltip")
            );
            bool newIncludeDesc = EditorGUILayout.Toggle(includeDescContent, includeDescriptionForTranslation);
            if (newIncludeDesc != includeDescriptionForTranslation)
            {
                if (translateWithFeishuDescription && !newIncludeDesc)
                {
                    if (EditorUtility.DisplayDialog(
                        I18N.T("Confirm"),
                        I18N.CurrentLanguage == I18N.Language.Chinese ?
                            "您取消\"携带描述提高翻译精准度\"后，会不携带描述去仅翻译有描述条目" :
                            "After canceling \"Include Description for Better Accuracy\", descriptions will not be included when translating items with descriptions",
                        I18N.T("OK"),
                        I18N.T("Cancel")
                    ))
                    {
                        includeDescriptionForTranslation = newIncludeDesc;
                    }
                }
                else
                {
                    includeDescriptionForTranslation = newIncludeDesc;
                }
            }

            // 仅翻译有描述的条目选项
            bool newTranslateWithDesc = EditorGUILayout.Toggle(
                new GUIContent(I18N.T("TranslateOnlyDescribed"), I18N.T("DescribedTooltip")),
                translateWithFeishuDescription
            );
            if (newTranslateWithDesc != translateWithFeishuDescription)
            {
                translateWithFeishuDescription = newTranslateWithDesc;
                if (translateWithFeishuDescription && !includeDescriptionForTranslation)
                {
                    includeDescriptionForTranslation = true;
                }
            }

            // 仅翻译审阅不合格项目
            translateOnlyFailed = EditorGUILayout.Toggle(
                new GUIContent(I18N.T("TranslateOnlyFailed"),
                    I18N.CurrentLanguage == I18N.Language.Chinese ? "仅对飞书审阅不合格的条目进行翻译" : "Translate only items failed in Feishu review"),
                translateOnlyFailed
            );

            // 携带审阅意见进行重新翻译
            translateWithReviewComments = EditorGUILayout.Toggle(
                new GUIContent(I18N.T("TranslateWithReviewComments"),
                    I18N.CurrentLanguage == I18N.Language.Chinese ? "把飞书审阅意见作为提示上下文参与翻译" : "Include Feishu review comments in translation context"),
                translateWithReviewComments
            );

            EditorGUILayout.Space(10);
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, I18N.T("TargetLanguageSelection"));
            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                var keys = languageToggles.Keys.ToList();
                foreach (var langCode in keys)
                {
                    var locale = locales.First(l => l.Identifier.Code == langCode);
                    languageToggles[langCode] = EditorGUILayout.Toggle(
                        $"{locale.LocaleName} ({langCode})",
                        languageToggles[langCode]
                    );
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(20);

            if (!isTranslating)
            {
                GUI.enabled = selectedCollection != null && languageToggles.Any(kvp => kvp.Value);
                if (GUILayout.Button(I18N.T("StartTranslation"), GUILayout.ExpandWidth(true)))
                {
                    StartTranslation();
                }
                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(statusMessage, EditorStyles.boldLabel);
                
                // 使用自定义的进度条样式
                var progressRect = EditorGUILayout.GetControlRect(false, 20);
                var progressBarRect = new Rect(progressRect.x + 2, progressRect.y + 2, progressRect.width - 4, progressRect.height - 4);
                
                // 绘制背景
                EditorGUI.DrawRect(progressBarRect, new Color(0.2f, 0.2f, 0.2f));
                
                // 绘制进度条
                var fillRect = new Rect(progressBarRect.x, progressBarRect.y, progressBarRect.width * translationProgress, progressBarRect.height);
                EditorGUI.DrawRect(fillRect, new Color(0.2f, 0.7f, 0.2f));
                
                // 绘制进度文本
                var style = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                EditorGUI.LabelField(progressBarRect, $"{(translationProgress * 100):F1}%", style);
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button(isPaused ? I18N.T("ContinueTranslation") : I18N.T("CancelTranslation")))
                {
                    if (isPaused)
                    {
                        // 如果当前已暂停，询问是否继续
                        if (EditorUtility.DisplayDialog(
                            I18N.T("ConfirmContinue"),
                            I18N.T("ConfirmContinueMessage"),
                            I18N.T("OK"),
                            I18N.T("Cancel")
                        ))
                        {
                            isPaused = false;
                            pauseCompletionSource?.TrySetResult(false); // false 表示继续
                        }
                    }
                    else
                    {
                        // 如果当前正在运行，询问是否取消
                        isPaused = true;
                        pauseCompletionSource = new TaskCompletionSource<bool>();
                        
                        if (EditorUtility.DisplayDialog(
                            I18N.T("ConfirmCancel"),
                            I18N.T("ConfirmCancelMessage"),
                            I18N.T("OK"),
                            I18N.T("Cancel")
                        ))
                        {
                            _cancellationTokenSource?.Cancel();
                            pauseCompletionSource?.TrySetResult(true); // true 表示取消
                        }
                        else
                        {
                            isPaused = false;
                            pauseCompletionSource?.TrySetResult(false);
                        }
                    }
                }
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField(I18N.T("ReviewOptions"), EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField(I18N.T("ReviewPrompt"));
            if (string.IsNullOrEmpty(reviewConstraints))
            {
                reviewConstraints = I18N.T("DefaultReviewPrompt");
            }
            if (reviewTextAreaStyle == null)
            {
                reviewTextAreaStyle = new GUIStyle(EditorStyles.textArea);
                reviewTextAreaStyle.wordWrap = true;
            }
            var content = new GUIContent(reviewConstraints);
            float calcWidth = position.width - 40f;
            float calcHeight = reviewTextAreaStyle.CalcHeight(content, calcWidth);
            reviewConstraints = EditorGUILayout.TextArea(reviewConstraints, reviewTextAreaStyle, GUILayout.Height(Mathf.Max(60f, calcHeight)), GUILayout.ExpandWidth(true));
            reviewOnlyNonEmpty = EditorGUILayout.Toggle(I18N.T("ReviewOnlyNonEmpty"), reviewOnlyNonEmpty);
            reviewOnlyEmptyReview = EditorGUILayout.Toggle(I18N.T("ReviewOnlyEmptyReview"), reviewOnlyEmptyReview);
            reviewOnlyDescribed = EditorGUILayout.Toggle(I18N.T("ReviewOnlyDescribed"), reviewOnlyDescribed);
            outputToFeishuReview = EditorGUILayout.Toggle(I18N.T("OutputToFeishuReview"), outputToFeishuReview);
            outputToFeishuReviewIncremental = EditorGUILayout.Toggle(I18N.T("OutputToFeishuReviewIncremental"), outputToFeishuReviewIncremental);
            EditorGUILayout.EndVertical();

            if (!isReviewing)
            {
                GUI.enabled = selectedCollection != null && languageToggles.Any(kvp => kvp.Value);
                var reviewTooltip = I18N.CurrentLanguage == I18N.Language.Chinese
                    ? "审阅：从飞书读取并审阅飞书内容，写回飞书审阅列。"
                    : "Review: read from Feishu and review Feishu content, write back review columns.";
                if (GUILayout.Button(new GUIContent(I18N.T("StartReview"), reviewTooltip), GUILayout.ExpandWidth(true)))
                {
                    StartFeishuReview();
                }
                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(reviewStatusMessage, EditorStyles.boldLabel);
                var progressRect2 = EditorGUILayout.GetControlRect(false, 20);
                var progressBarRect2 = new Rect(progressRect2.x + 2, progressRect2.y + 2, progressRect2.width - 4, progressRect2.height - 4);
                EditorGUI.DrawRect(progressBarRect2, new Color(0.2f, 0.2f, 0.2f));
                var fillRect2 = new Rect(progressBarRect2.x, progressBarRect2.y, progressBarRect2.width * reviewProgress, progressBarRect2.height);
                EditorGUI.DrawRect(fillRect2, new Color(0.2f, 0.5f, 0.7f));
                var style2 = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                EditorGUI.LabelField(progressBarRect2, $"{(reviewProgress * 100):F1}%", style2);
                EditorGUILayout.Space(5);
                if (GUILayout.Button(isPaused ? I18N.T("ContinueReview") : I18N.T("CancelReview"), GUILayout.ExpandWidth(true)))
                {
                    if (isPaused)
                    {
                        if (EditorUtility.DisplayDialog(
                            I18N.T("ConfirmContinue"),
                            I18N.T("ConfirmContinueMessage"),
                            I18N.T("OK"),
                            I18N.T("Cancel")
                        ))
                        {
                            isPaused = false;
                            pauseCompletionSource?.TrySetResult(false);
                        }
                    }
                    else
                    {
                        isPaused = true;
                        pauseCompletionSource = new TaskCompletionSource<bool>();
                        if (EditorUtility.DisplayDialog(
                            I18N.T("ConfirmCancel"),
                            I18N.T("ConfirmCancelMessage"),
                            I18N.T("OK"),
                            I18N.T("Cancel")
                        ))
                        {
                            _cancellationTokenSource?.Cancel();
                            pauseCompletionSource?.TrySetResult(true);
                        }
                        else
                        {
                            isPaused = false;
                            pauseCompletionSource?.TrySetResult(false);
                        }
                    }
                }
            }

            // 移除自动修复进度显示

            EditorGUILayout.EndScrollView();
        }

        private async void StartTranslation()
        {
            if (selectedCollection == null) return;

            var targetLanguages = languageToggles
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            if (targetLanguages.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "请至少选择一个目标语言", "确定");
                return;
            }

            var sourceTable = selectedCollection.GetTable(sourceLanguage) as StringTable;
            if (sourceTable == null)
            {
                EditorUtility.DisplayDialog("错误", $"找不到源语言({sourceLanguage})的表格", "确定");
                return;
            }

            // 如果需要从飞书获取描述
            if (includeDescriptionForTranslation || translateWithFeishuDescription)
            {
                try
                {
                    statusMessage = I18N.T("LoadingData");
                    translationProgress = 0f;
                    Repaint();

                    // 获取飞书表格
                    var tables = await _feishuService.ListTables();
                    var matchingTable = tables.FirstOrDefault(t => t["name"].Value<string>() == selectedCollection.TableCollectionName);
                    
                    if (matchingTable != null)
                    {
                        var tableId = matchingTable["table_id"].Value<string>();
                        var records = await _feishuService.ListRecords(tableId);
                        
                        // 清空并重新填充描述字典
                        _keyDescriptions.Clear();
                        foreach (var record in records)
                        {
                            var fields = record["fields"] as JObject;
                            var key = fields["Key"]?.Value<string>();
                            var description = fields["Description"]?.Value<string>();
                            
                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(description))
                            {
                                _keyDescriptions[key] = description;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"获取飞书描述时发生错误: {ex}");
                    EditorUtility.DisplayDialog(I18N.T("Error"), 
                        I18N.CurrentLanguage == I18N.Language.Chinese ? 
                            "获取飞书描述时发生错误，请检查控制台获取详细信息" : 
                            "Error getting descriptions from Feishu, please check the console for details", 
                        I18N.T("OK"));
                    return;
                }
            }

            // 在主线程预先获取所有需要的信息
            var translationTasks = new List<(string langCode, string langName, long entryId, string sourceText, StringTable targetTable, string reviewFeedback)>();
            var targetTables = new Dictionary<string, StringTable>();
            Dictionary<string, Dictionary<string, string>> reviewByKeyLang = null;
            if (translateOnlyFailed || translateWithReviewComments)
            {
                try
                {
                    var tables2 = await _feishuService.ListTables();
                    var matchingTable2 = tables2.FirstOrDefault(t => t["name"].Value<string>() == selectedCollection.TableCollectionName);
                    if (matchingTable2 != null)
                    {
                        var tableId2 = matchingTable2["table_id"].Value<string>();
                        var records2 = await _feishuService.ListRecords(tableId2);
                        reviewByKeyLang = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                        foreach (var record in records2)
                        {
                            var fields = record["fields"] as JObject;
                            var key = fields["Key"]?.Value<string>();
                            if (string.IsNullOrEmpty(key)) continue;
                            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var lang in languageToggles.Keys)
                            {
                                var rv = fields[$"Review_{lang}"]?.Value<string>();
                                if (!string.IsNullOrEmpty(rv)) dict[lang] = rv;
                            }
                            if (dict.Count > 0) reviewByKeyLang[key] = dict;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"读取飞书审阅数据失败: {ex}");
                }
            }
            
            foreach (var langCode in targetLanguages)
            {
                var table = selectedCollection.GetTable(langCode) as StringTable;
                if (table == null) continue;
                targetTables[langCode] = table;

                var locale = LocalizationEditorSettings.GetLocale(langCode);
                string languageName = locale?.LocaleName ?? langCode;

                foreach (var entry in sourceTable.SharedData.Entries)
                {
                    var sourceEntry = sourceTable.GetEntry(entry.Id);
                    if (sourceEntry == null || string.IsNullOrEmpty(sourceEntry.Value)) continue;

                    var targetEntry = table.GetEntry(entry.Id);
                    bool shouldTranslate = true;

                    // 检查是否满足"仅翻译空值"条件
                    if (translateEmptyOnly)
                    {
                        shouldTranslate = targetEntry == null || string.IsNullOrEmpty(targetEntry.Value);
                    }

                    // 检查是否满足"仅翻译有描述的条目"条件
                    if (shouldTranslate && translateWithFeishuDescription)
                    {
                        shouldTranslate = _keyDescriptions.ContainsKey(entry.Key);
                    }

                    // 可选：仅翻译审阅不合格项目
                    string reviewFeedback = null;
                    if (shouldTranslate && (translateOnlyFailed || translateWithReviewComments) && reviewByKeyLang != null)
                    {
                        if (reviewByKeyLang.TryGetValue(entry.Key, out var langMap) && langMap.TryGetValue(langCode, out var rv))
                        {
                            reviewFeedback = rv;
                            if (translateOnlyFailed)
                            {
                                shouldTranslate = !IsOkText(rv);
                            }
                        }
                        else if (translateOnlyFailed)
                        {
                            shouldTranslate = false;
                        }
                    }

                    // 如果满足所有启用的条件，则添加到翻译任务中
                    if (shouldTranslate)
                    {
                        translationTasks.Add((langCode, languageName, entry.Id, sourceEntry.Value, table, reviewFeedback));
                    }
                }
            }

            if (translationTasks.Count == 0)
            {
                EditorUtility.DisplayDialog(I18N.T("Confirm"), I18N.T("NoItemsToTranslate"), I18N.T("OK"));
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                I18N.T("ConfirmTranslation"),
                string.Format(I18N.T("ConfirmTranslationMessage"), translationTasks.Count),
                I18N.T("OK"),
                I18N.T("Cancel")
            );

            if (!confirmed) return;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            isTranslating = true;
            translationProgress = 0f;
            completedTranslations = 0;
            totalTranslations = translationTasks.Count;
            hasShownResult = false;  // 重置结果显示状态
            isPaused = false;        // 重置暂停状态
            pauseCompletionSource = null;

            try
            {
                // 创建信号量来控制并发请求数
                using (var semaphore = new SemaphoreSlim(LLMAIConfig.Instance.maxConcurrentRequests))
                {
                    var remainingTasks = new List<(string langCode, string langName, long entryId, string sourceText, StringTable targetTable, string reviewFeedback)>(translationTasks);
                    var processedTasks = new HashSet<(string langCode, long entryId)>();
                    var currentBatchTasks = new List<Task>();
                    
                    Debug.Log($"开始翻译，共 {translationTasks.Count} 个任务");
                    
                    for (int retryCount = 0; retryCount < LLMAIConfig.Instance.maxRetries && remainingTasks.Count > 0; retryCount++)
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            ShowTranslationResult(true);
                            return;
                        }

                        if (retryCount > 0)
                        {
                            int delaySeconds = (int)(LLMAIConfig.Instance.retryDelaySeconds * Math.Pow(2.0, retryCount - 1));
                            Debug.Log($"等待 {delaySeconds} 秒后开始第 {retryCount + 1} 轮翻译，待处理条目数：{remainingTasks.Count}");
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), _cancellationTokenSource.Token);
                        }

                        var currentTasks = new List<(string langCode, string langName, long entryId, string sourceText, StringTable targetTable, string reviewFeedback)>(remainingTasks);
                        remainingTasks.Clear();

                        // 替换 Chunk 方法，使用自定义的批处理逻辑
                        for (int i = 0; i < currentTasks.Count; i += LLMAIConfig.Instance.maxConcurrentRequests)
                        {
                            if (_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                ShowTranslationResult(true);
                                return;
                            }

                            currentBatchTasks.Clear();
                            var batchSize = Math.Min(LLMAIConfig.Instance.maxConcurrentRequests, currentTasks.Count - i);
                            for (int j = 0; j < batchSize; j++)
                            {
                                var task = currentTasks[i + j];
                                var translationTask = ProcessTranslationTask(
                                    task,
                                    semaphore,
                                    processedTasks,
                                    remainingTasks,
                                    retryCount,
                                    _cancellationTokenSource.Token
                                );
                                currentBatchTasks.Add(translationTask);
                            }

                            try
                            {
                                await Task.WhenAll(currentBatchTasks);
                                // 每批次处理后的短暂延迟
                                await Task.Delay(200, _cancellationTokenSource.Token);
                            }
                            catch (Exception ex)
                            {
                                if (ex is OperationCanceledException)
                                {
                                    throw;
                                }
                                Debug.LogError($"批次处理出错: {ex.Message}");
                            }

                            // 强制重绘窗口以更新进度
                            Repaint();
                        }

                        if (remainingTasks.Count == 0)
                        {
                            Debug.Log($"第 {retryCount + 1} 轮处理完成，所有任务都已成功");
                            break;
                        }
                        else if (retryCount < LLMAIConfig.Instance.maxRetries - 1)
                        {
                            Debug.Log($"第 {retryCount + 1} 轮处理完成，还有 {remainingTasks.Count} 个任务需要重试");
                        }
                        else
                        {
                            Debug.LogError($"已完成所有 {LLMAIConfig.Instance.maxRetries} 轮重试，仍有 {remainingTasks.Count} 个任务失败");
                        }
                    }

                    // 确保最后一次保存
                    await SaveAssetsAsync();
                    ShowTranslationResult(false);
                }
            }
            catch (Exception e)
            {
                if (!(e is OperationCanceledException))
                {
                    Debug.LogError($"翻译过程中发生错误: {e}");
                    var errorMessage = $"翻译过程中发生错误: {e.Message}\n\n" +
                                     $"成功翻译: {completedTranslations} 个条目\n" +
                                     $"未完成: {totalTranslations - completedTranslations} 个条目\n" +
                                     $"总计: {totalTranslations} 个条目";
                    EditorUtility.DisplayDialog("错误", errorMessage, "确定");
                }
                else if (!hasShownResult)
                {
                    ShowTranslationResult(true);
                }
                isTranslating = false;
                _cancellationTokenSource = null;
                Repaint();
            }
        }

        private async Task ProcessTranslationTask(
            (string langCode, string langName, long entryId, string sourceText, StringTable targetTable, string reviewFeedback) task,
            SemaphoreSlim semaphore,
            HashSet<(string langCode, long entryId)> processedTasks,
            List<(string langCode, string langName, long entryId, string sourceText, StringTable targetTable, string reviewFeedback)> remainingTasks,
            int retryCount,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                // 检查是否需要暂停
                while (isPaused)
                {
                    if (pauseCompletionSource == null)
                    {
                        pauseCompletionSource = new TaskCompletionSource<bool>();
                    }
                    
                    bool shouldCancel = await pauseCompletionSource.Task;
                    if (shouldCancel)
                    {
                        throw new OperationCanceledException();
                    }
                }

                // 获取描述信息（如果有）
                string description = null;
                string additionalContext = null;
                var entryKey = selectedCollection.SharedData.Entries.FirstOrDefault(e => e.Id == task.entryId)?.Key;
                
                if (entryKey != null && includeDescriptionForTranslation)
                {
                    _keyDescriptions.TryGetValue(entryKey, out description);
                }
                
                // 构建本地化上下文信息
                if (includeLocalizationKey && !string.IsNullOrEmpty(entryKey))
                {
                    additionalContext = $"This text is associated with the key '{entryKey}' in a game localization system.";
                }

                // 创建一个带超时的翻译任务
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(LLMAIConfig.Instance.timeoutSeconds));

                var translationTask = LLMAIService.Instance.TranslateText(
                    task.sourceText,
                    task.langName,
                    entryKey,
                    description,
                    null,
                    null,
                    translateWithReviewComments && !string.IsNullOrEmpty(task.reviewFeedback)
                        ? (string.IsNullOrEmpty(additionalContext) ? ($"Review feedback: {task.reviewFeedback}") : (additionalContext + " | Review feedback: " + task.reviewFeedback))
                        : additionalContext
                );

                string translatedText;
                try
                {
                    // 使用 Task.WhenAny 来实现可取消的等待
                    var completedTask = await Task.WhenAny(translationTask, Task.Delay(-1, cancellationToken));
                    cancellationToken.ThrowIfCancellationRequested();

                    if (completedTask == translationTask)
                    {
                        translatedText = await translationTask;
                    }
                    else
                    {
                        throw new OperationCanceledException();
                    }
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                    {
                        if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                        {
                            throw new TimeoutException($"翻译超时 (超过 {LLMAIConfig.Instance.timeoutSeconds} 秒)");
                        }
                        throw;
                    }
                    throw;
                }

                if (string.IsNullOrEmpty(translatedText))
                {
                    // 如果源文本为空，则空的翻译结果是合理的
                    if (string.IsNullOrEmpty(task.sourceText))
                    {
                        // 在主线程中更新翻译结果(空字符串)
                        await UpdateTranslationResult(task, string.Empty, processedTasks);
                        return;
                    }
                    throw new Exception("翻译结果为空");
                }

                // 在主线程中更新翻译结果
                await UpdateTranslationResult(task, translatedText, processedTasks);
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException)
                {
                    throw;
                }

                string errorMessage = retryCount == LLMAIConfig.Instance.maxRetries - 1 
                    ? $"最后一次尝试失败 [{task.langCode}][{task.entryId}] {task.sourceText}: {e.Message}"
                    : $"第 {retryCount + 1} 轮尝试失败 [{task.langCode}][{task.entryId}] {task.sourceText}: {e.Message}";
                Debug.LogError(errorMessage);

                // 如果不是最后一次重试，添加到失败列表
                if (retryCount < LLMAIConfig.Instance.maxRetries - 1)
                {
                    lock (remainingTasks)
                    {
                        remainingTasks.Add(task);
                    }
                }
                else
                {
                    // 在最后一次重试失败时，如果还未计入进度，则计入
                    var taskKey = (task.langCode, task.entryId);
                    UpdateProgress(taskKey, processedTasks);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task UpdateTranslationResult(
            (string langCode, string langName, long entryId, string sourceText, StringTable targetTable, string reviewFeedback) task,
            string translatedText,
            HashSet<(string langCode, long entryId)> processedTasks)
        {
            var taskKey = (task.langCode, task.entryId);
            var tcs = new TaskCompletionSource<bool>();

            EditorApplication.delayCall += () =>
            {
                try
                {
                    var targetEntry = task.targetTable.GetEntry(task.entryId);
                    if (targetEntry == null)
                    {
                        targetEntry = task.targetTable.AddEntry(task.entryId, translatedText);
                    }
                    else
                    {
                        targetEntry.Value = translatedText;
                    }

                    EditorUtility.SetDirty(task.targetTable);
                    UpdateProgress(taskKey, processedTasks);
                    tcs.TrySetResult(true);
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            };

            await tcs.Task;
        }

        private void UpdateProgress((string langCode, long entryId) taskKey, HashSet<(string langCode, long entryId)> processedTasks)
        {
            lock (processedTasks)
            {
                if (!processedTasks.Contains(taskKey))
                {
                    completedTranslations++;
                    processedTasks.Add(taskKey);
                    translationProgress = (float)completedTranslations / totalTranslations;
                    statusMessage = string.Format(I18N.T("TranslatingProgress"), completedTranslations, totalTranslations);
                    Repaint();
                }
            }
        }

        private async Task SaveAssetsAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            EditorApplication.delayCall += () =>
            {
                try
                {
                    AssetDatabase.SaveAssets();
                    tcs.TrySetResult(true);
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            };
            await tcs.Task;
        }

        private void ShowTranslationResult(bool wasCancelled)
        {
            if (hasShownResult) return;
            
            hasShownResult = true;
            string resultMessage = string.Format(
                I18N.T("TranslationResult"), 
                wasCancelled ? I18N.T("Cancelled") : I18N.T("Completed"),
                completedTranslations,
                totalTranslations - completedTranslations,
                totalTranslations
            );

            var finalTitle = wasCancelled ? I18N.T("Cancelled") : I18N.T("Completed");

            EditorApplication.delayCall += () =>
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(finalTitle, resultMessage, I18N.T("OK"));
                isTranslating = false;
                _cancellationTokenSource = null;
                hasShownResult = false;
                Repaint();
            };
        }

        private IEnumerator WaitForTranslation(Task<string> translationTask, TaskCompletionSource<string> tcs)
        {
            while (!translationTask.IsCompleted)
            {
                yield return null;
            }

            if (translationTask.IsFaulted)
            {
                tcs.TrySetException(translationTask.Exception);
            }
            else
            {
                tcs.TrySetResult(translationTask.Result);
            }
        }

        private void SetSmartStringsByChineseContent()
        {
            if(selectedCollection == null) return;

            bool confirmed = EditorUtility.DisplayDialog(
                "确认设置Smart String", 
                "此操作将根据中文内容中是否包含变量标记（如 {0}、@、{name} 等）来设置所有语言的Smart String状态。\n\n是否继续？", 
                "确定", 
                "取消"
            );

            if(!confirmed) return;

            try
            {
                EditorUtility.DisplayProgressBar("处理中", "正在分析中文内容...", 0f);

                var zhTable = selectedCollection.GetTable("zh-CN") as StringTable;
                if(zhTable == null)
                {
                    EditorUtility.DisplayDialog("错误", "找不到中文语言表", "确定");
                    return;
                }

                var allLanguages = LocalizationSettings.AvailableLocales.Locales;
                int totalEntries = zhTable.SharedData.Entries.Count();
                int processedEntries = 0;

                foreach(var entry in zhTable.SharedData.Entries)
                {
                    var zhEntry = zhTable.GetEntry(entry.Id);
                    if(zhEntry == null) continue;

                    // 检查中文内容是否包含变量标记
                    bool shouldEnableSmart = zhEntry.Value?.Contains("{") == true || 
                                           zhEntry.Value?.Contains("@") == true;

                    // 为所有语言设置Smart状态
                    foreach(var locale in allLanguages)
                    {
                        var table = selectedCollection.GetTable(locale.Identifier) as StringTable;
                        if(table == null) continue;

                        var targetEntry = table.GetEntry(entry.Id);
                        if(targetEntry == null)
                        {
                            // 如果条目不存在，创建一个空条目
                            targetEntry = table.AddEntry(entry.Id, "");
                        }
                        targetEntry.IsSmart = shouldEnableSmart;
                        EditorUtility.SetDirty(table);
                    }

                    processedEntries++;
                    EditorUtility.DisplayProgressBar(
                        "处理中", 
                        $"正在处理... ({processedEntries}/{totalEntries})", 
                        processedEntries / (float)totalEntries
                    );
                }

                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("完成", "Smart String设置已完成", "确定");
            }
            catch(Exception e)
            {
                Debug.LogError($"设置Smart String时出错: {e}");
                EditorUtility.DisplayDialog("错误", $"设置Smart String时出错：{e.Message}", "确定");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void HandleTranslation()
        {
            // Implementation of HandleTranslation method
        }

        private async void StartFeishuReview()
        {
            if (selectedCollection == null) return;

            var targetLanguages = languageToggles
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            if (targetLanguages.Count == 0)
            {
                EditorUtility.DisplayDialog(I18N.T("Error"), I18N.T("NoItemsToTranslate"), I18N.T("OK"));
                return;
            }

            // 从飞书读取审阅数据源

            try
            {
                reviewStatusMessage = I18N.T("LoadingData");
                reviewProgress = 0f;
                Repaint();
                var tables = await _feishuService.ListTables();
                var matchingTable = tables.FirstOrDefault(t => t["name"].Value<string>() == selectedCollection.TableCollectionName);
                if (matchingTable == null)
                {
                    EditorUtility.DisplayDialog(I18N.T("Error"), $"飞书中未找到表: {selectedCollection.TableCollectionName}", I18N.T("OK"));
                    return;
                }
                var tableId = matchingTable["table_id"].Value<string>();
                var records = await _feishuService.GetAllRecords(tableId);
                _keyDescriptions.Clear();
                foreach (var record in records)
                {
                    var fields = record["fields"] as JObject;
                    var key = fields["Key"]?.Value<string>();
                    var description = fields["Description"]?.Value<string>();
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(description))
                    {
                        _keyDescriptions[key] = description;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"读取飞书数据时发生错误: {ex}");
                EditorUtility.DisplayDialog(I18N.T("Error"), I18N.T("LoadingData"), I18N.T("OK"));
                return;
            }

            var reviewTasks = new List<(string langCode, string langName, string entryKey, string sourceText, string targetText)>();
            var locales = LocalizationEditorSettings.GetLocales();
            var localeMap = locales.ToDictionary(l => l.Identifier.Code, l => l.LocaleName);
            var tables2 = await _feishuService.ListTables();
            var matchingTable2 = tables2.FirstOrDefault(t => t["name"].Value<string>() == selectedCollection.TableCollectionName);
            var tableId2 = matchingTable2["table_id"].Value<string>();
            var records2 = await _feishuService.GetAllRecords(tableId2);
            foreach (var record in records2)
            {
                var fields = record["fields"] as JObject;
                var key = fields["Key"]?.Value<string>();
                if (string.IsNullOrEmpty(key)) continue;
                var sourceText = fields[sourceLanguage]?.Value<string>();
                foreach (var langCode in targetLanguages)
                {
                    var targetText = fields[langCode]?.Value<string>();
                    var currentReview = fields[$"Review_{langCode}"]?.Value<string>();
                    if (reviewOnlyNonEmpty && string.IsNullOrEmpty(targetText)) continue;
                    if (reviewOnlyEmptyReview && !string.IsNullOrEmpty(currentReview)) continue;
                    if (reviewOnlyDescribed && !_keyDescriptions.ContainsKey(key)) continue;
                    var langName = localeMap.TryGetValue(langCode, out var nm) ? nm : langCode;
                    reviewTasks.Add((langCode, langName, key, sourceText, targetText));
                }
            }

            if (reviewTasks.Count == 0)
            {
                EditorUtility.DisplayDialog(I18N.T("Confirm"), I18N.T("NoItemsToTranslate"), I18N.T("OK"));
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                I18N.T("ConfirmReview"),
                string.Format(I18N.T("ConfirmReviewMessage"), reviewTasks.Count),
                I18N.T("OK"),
                I18N.T("Cancel")
            );
            if (!confirmed) return;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            isReviewing = true;
            reviewProgress = 0f;
            completedReviews = 0;
            totalReviews = reviewTasks.Count;
            Debug.Log($"FeishuReview: Created {totalReviews} review tasks.");
            isPaused = false;
            pauseCompletionSource = null;
            _reviewIssuesByKey.Clear();
            _lastFlushedCounts.Clear();

            try
            {
                using (var semaphore = new SemaphoreSlim(LLMAIConfig.Instance.maxConcurrentRequests))
                {
                    var remainingTasks = new List<(string langCode, string langName, string entryKey, string sourceText, string targetText)>(reviewTasks);
                    var processedTasks = new HashSet<(string langCode, string entryKey)>();
                    var currentBatchTasks = new List<Task>();

                    for (int retryCount = 0; retryCount < LLMAIConfig.Instance.maxRetries && remainingTasks.Count > 0; retryCount++)
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            await FlushFeishuReviewPartial(null);
                            ShowReviewResult(true);
                            return;
                        }

                        if (retryCount > 0)
                        {
                            int delaySeconds = (int)(LLMAIConfig.Instance.retryDelaySeconds * Math.Pow(2.0, retryCount - 1));
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), _cancellationTokenSource.Token);
                        }

                        var currentTasks = new List<(string langCode, string langName, string entryKey, string sourceText, string targetText)>(remainingTasks);
                        remainingTasks.Clear();

                        for (int i = 0; i < currentTasks.Count; i += LLMAIConfig.Instance.maxConcurrentRequests)
                        {
                            if (_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                await FlushFeishuReviewPartial(null);
                                ShowReviewResult(true);
                                return;
                            }

                            currentBatchTasks.Clear();
                            var batchSize = Math.Min(LLMAIConfig.Instance.maxConcurrentRequests, currentTasks.Count - i);
                            for (int j = 0; j < batchSize; j++)
                            {
                                var task = currentTasks[i + j];
                                var t = ProcessFeishuReviewTask(
                                    task,
                                    semaphore,
                                    processedTasks,
                                    remainingTasks,
                                    retryCount,
                                    _cancellationTokenSource.Token
                                );
                                currentBatchTasks.Add(t);
                            }

                            try
                            {
                                await Task.WhenAll(currentBatchTasks);
                                await Task.Delay(200, _cancellationTokenSource.Token);
                                await FlushFeishuReviewPartial(null);
                            }
                            catch (Exception ex)
                            {
                                if (ex is OperationCanceledException) throw;
                            }
                            Repaint();
                        }

                        if (remainingTasks.Count == 0) break;
                    }

                    await SaveAssetsAsync();
                    if (outputToFeishuReview)
                    {
                        var sync = new LocalizationSyncManager();
                        var map = new Dictionary<string, string>();
                        var statusMap = new Dictionary<string, string>();
                        foreach (var kv in _reviewIssuesByKey)
                        {
                            if (kv.Value == null || kv.Value.Count == 0) continue;
                            var joined = string.Join("\n", kv.Value);
                            map[kv.Key] = string.IsNullOrEmpty(joined) ? "OK" : joined;
                            int failCount = kv.Value.Count(l => !IsOkText(l));
                            statusMap[kv.Key] = failCount == 0 ? "通过" : $"未通过({failCount})";
                        }
                        await sync.UpdateReviewForTable(selectedCollection, map, statusMap);
                    }
                    ShowReviewResult(false);
                }
            }
            catch (Exception e)
            {
                if (!(e is OperationCanceledException))
                {
                    var errorMessage = $"审阅过程中发生错误: {e.Message}\n\n" +
                                      $"成功审阅: {completedReviews} 个条目\n" +
                                      $"未完成: {totalReviews - completedReviews} 个条目\n" +
                                      $"总计: {totalReviews} 个条目";
                    EditorUtility.DisplayDialog(I18N.T("Error"), errorMessage, I18N.T("OK"));
                }
                else if (!hasShownResult)
                {
                    ShowReviewResult(true);
                }
                isReviewing = false;
                _cancellationTokenSource = null;
                Repaint();
            }
        }

        // 自动修复功能已移除

        private static System.Text.RegularExpressions.Regex _reNumeric = new System.Text.RegularExpressions.Regex("\\{[0-9]+\\}", System.Text.RegularExpressions.RegexOptions.Compiled);
        private static System.Text.RegularExpressions.Regex _reNamed = new System.Text.RegularExpressions.Regex("\\{[A-Za-z_][A-Za-z0-9_]*\\}", System.Text.RegularExpressions.RegexOptions.Compiled);
        private static System.Text.RegularExpressions.Regex _rePercent = new System.Text.RegularExpressions.Regex("%[dsf]", System.Text.RegularExpressions.RegexOptions.Compiled);

        private bool ValidatePlaceholders(string originalTarget, string fixedTarget)
        {
            var set = new HashSet<string>();
            foreach (System.Text.RegularExpressions.Match m in _reNumeric.Matches(originalTarget ?? string.Empty)) set.Add(m.Value);
            foreach (System.Text.RegularExpressions.Match m in _reNamed.Matches(originalTarget ?? string.Empty)) set.Add(m.Value);
            foreach (System.Text.RegularExpressions.Match m in _rePercent.Matches(originalTarget ?? string.Empty)) set.Add(m.Value);
            foreach (var ph in set)
            {
                if (!(fixedTarget ?? string.Empty).Contains(ph)) return false;
            }
            return true;
        }

        private bool IsOkText(string text)
        {
            var t = (text ?? string.Empty).Trim();
            if (string.Equals(t, "OK", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.EndsWith("OK")) return true;
            if (t.Contains(": OK")) return true;
            return false;
        }

        

        

        // 原基于Unity表的审阅流程已移除

        private async Task UpdateReviewResult(string entryKey, string line,
            HashSet<(string langCode, long entryId)> processedTasks,
            (string langCode, long entryId) taskKey)
        {
            var tcs = new TaskCompletionSource<bool>();
            EditorApplication.delayCall += () =>
            {
                if (!string.IsNullOrEmpty(entryKey))
                {
                    if (!_reviewIssuesByKey.TryGetValue(entryKey, out var list))
                    {
                        list = new List<string>();
                        _reviewIssuesByKey[entryKey] = list;
                    }
                    if (!string.IsNullOrEmpty(line)) list.Add(line);
                }
                UpdateReviewProgress(taskKey, processedTasks);
                tcs.TrySetResult(true);
            };
            await tcs.Task;
        }

        private void UpdateReviewProgress((string langCode, long entryId) taskKey, HashSet<(string langCode, long entryId)> processedTasks)
        {
            lock (processedTasks)
            {
                if (!processedTasks.Contains(taskKey))
                {
                    completedReviews++;
                    processedTasks.Add(taskKey);
                    reviewProgress = (float)completedReviews / totalReviews;
                    reviewStatusMessage = string.Format(I18N.T("ReviewingProgress"), completedReviews, totalReviews);
                    Repaint();
                }
            }
        }

        private void ShowReviewResult(bool wasCancelled)
        {
            var finalTitle = wasCancelled ? I18N.T("Cancelled") : I18N.T("Completed");
            var resultMessage = string.Format(I18N.T("ReviewResult"), wasCancelled ? I18N.T("Cancelled") : I18N.T("Completed"), completedReviews, totalReviews - completedReviews, totalReviews);
            EditorApplication.delayCall += () =>
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(finalTitle, resultMessage, I18N.T("OK"));
                isReviewing = false;
                _cancellationTokenSource = null;
                Repaint();
            };
        }

        private async Task FlushFeishuReviewPartial(HashSet<string> keys)
        {
            if (!outputToFeishuReview || !outputToFeishuReviewIncremental || selectedCollection == null) return;
            var sync = new LocalizationSyncManager();
            var map = new Dictionary<string, string>();
            var statusMap = new Dictionary<string, string>();
            foreach (var kv in _reviewIssuesByKey)
            {
                var key = kv.Key;
                if (keys != null && !keys.Contains(key)) continue;
                var count = kv.Value?.Count ?? 0;
                int prev = _lastFlushedCounts.TryGetValue(key, out var c) ? c : -1;
                if (count <= 0 || count == prev) continue;
                var joined = string.Join("\n", kv.Value);
                map[key] = string.IsNullOrEmpty(joined) ? "OK" : joined;
                int failCount = kv.Value.Count(l => !IsOkText(l));
                statusMap[key] = failCount == 0 ? "通过" : $"未通过({failCount})";
            }
            if (map.Count > 0)
            {
                await sync.UpdateReviewForTable(selectedCollection, map, statusMap);
                foreach (var key in map.Keys)
                {
                    _lastFlushedCounts[key] = _reviewIssuesByKey.TryGetValue(key, out var list) ? (list?.Count ?? 0) : 0;
                }
            }
        }

        private async Task ProcessFeishuReviewTask(
            (string langCode, string langName, string entryKey, string sourceText, string targetText) task,
            SemaphoreSlim semaphore,
            HashSet<(string langCode, string entryKey)> processedTasks,
            List<(string langCode, string langName, string entryKey, string sourceText, string targetText)> remainingTasks,
            int retryCount,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                while (isPaused)
                {
                    if (pauseCompletionSource == null) pauseCompletionSource = new TaskCompletionSource<bool>();
                    bool shouldCancel = await pauseCompletionSource.Task;
                    if (shouldCancel) throw new OperationCanceledException();
                }

                string description = null;
                string additionalContext = null;
                if (includeDescriptionForTranslation || reviewOnlyDescribed)
                {
                    _keyDescriptions.TryGetValue(task.entryKey, out description);
                }
                if (includeLocalizationKey && !string.IsNullOrEmpty(task.entryKey))
                {
                    additionalContext = $"This text is associated with the key '{task.entryKey}' in a game localization system.";
                }

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(LLMAIConfig.Instance.timeoutSeconds));

                var reviewTask = LLMAIService.Instance.ReviewTranslation(
                    task.sourceText,
                    task.targetText,
                    LocalizationEditorSettings.GetLocale(sourceLanguage)?.LocaleName ?? sourceLanguage,
                    task.langName,
                    task.entryKey,
                    description,
                    reviewConstraints,
                    additionalContext
                );

                var completedTask = await Task.WhenAny(reviewTask, Task.Delay(-1, cancellationToken));
                cancellationToken.ThrowIfCancellationRequested();
                string reviewText = completedTask == reviewTask ? await reviewTask : string.Empty;

                var formatted = string.IsNullOrEmpty(reviewText) ? $"{task.langCode}: OK" : $"{task.langCode}: {reviewText}";
                await UpdateReviewResultFeishu(task.entryKey, formatted, processedTasks, (task.langCode, task.entryKey));
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException) throw;
                UpdateReviewProgressFeishu((task.langCode, task.entryKey), processedTasks);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task UpdateReviewResultFeishu(string entryKey, string line,
            HashSet<(string langCode, string entryKey)> processedTasks,
            (string langCode, string entryKey) taskKey)
        {
            var tcs = new TaskCompletionSource<bool>();
            EditorApplication.delayCall += () =>
            {
                if (!string.IsNullOrEmpty(entryKey))
                {
                    if (!_reviewIssuesByKey.TryGetValue(entryKey, out var list))
                    {
                        list = new List<string>();
                        _reviewIssuesByKey[entryKey] = list;
                    }
                    if (!string.IsNullOrEmpty(line)) list.Add(line);
                }
                UpdateReviewProgressFeishu(taskKey, processedTasks);
                tcs.TrySetResult(true);
            };
            await tcs.Task;
        }

        private void UpdateReviewProgressFeishu((string langCode, string entryKey) taskKey, HashSet<(string langCode, string entryKey)> processedTasks)
        {
            lock (processedTasks)
            {
                if (!processedTasks.Contains(taskKey))
                {
                    completedReviews++;
                    processedTasks.Add(taskKey);
                    reviewProgress = (float)completedReviews / totalReviews;
                    reviewStatusMessage = string.Format(I18N.T("ReviewingProgress"), completedReviews, totalReviews);
                    Repaint();
                }
            }
        }
    }
}