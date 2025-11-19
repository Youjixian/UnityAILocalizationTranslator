using UnityEngine;
using UnityEditor;
using System;

namespace CardGame.Editor.LLMAI
{
    public class LLMAIConfigWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private GUIStyle promptTextAreaStyle;

        [MenuItem("Tools/Localization/AI Translator Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<LLMAIConfigWindow>(I18N.T("AISettings"));
            window.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(I18N.T("AISettings"), EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            string newApiUrl = EditorGUILayout.TextField(I18N.T("APIURL"), LLMAIConfig.Instance.apiUrl);
            
            EditorGUILayout.BeginHorizontal();
            string newApiKey = EditorGUILayout.PasswordField(I18N.T("APIKey"), LLMAIConfig.Instance.apiKey);
            if(GUILayout.Button(I18N.T("TestConnection"), GUILayout.Width(80)))
            {
                TestConnection();
            }
            EditorGUILayout.EndHorizontal();

            string newModelName = EditorGUILayout.TextField(I18N.T("ModelName"), LLMAIConfig.Instance.modelName);
            bool newUseMaxCompletionTokens = EditorGUILayout.Toggle(
                new GUIContent(I18N.T("UseMaxCompletionTokens"), I18N.T("UseMaxCompletionTokensDesc")),
                LLMAIConfig.Instance.useMaxCompletionTokens
            );
            float newTemperature = EditorGUILayout.Slider(
                new GUIContent(I18N.T("Temperature"), I18N.T("TemperatureDesc")),
                LLMAIConfig.Instance.temperature,
                0f,
                2f
            );
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(I18N.T("PerformanceSettings"), EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            int newMaxConcurrentRequests = EditorGUILayout.IntSlider(
                new GUIContent(
                    I18N.T("MaxConcurrentRequests"),
                    I18N.T("MaxRetriesDescription")
                ),
                LLMAIConfig.Instance.maxConcurrentRequests,
                1,
                10
            );

            int newMaxRetries = EditorGUILayout.IntSlider(
                new GUIContent(
                    I18N.T("MaxRetriesCount"),
                    I18N.T("MaxRetriesDescription")
                ),
                LLMAIConfig.Instance.maxRetries,
                0,
                5
            );

            float newRetryDelay = EditorGUILayout.Slider(
                new GUIContent(
                    I18N.T("RetryDelay"),
                    I18N.T("RetryDelayDescription")
                ),
                LLMAIConfig.Instance.retryDelaySeconds,
                0.1f,
                5f
            );

            float newTimeout = EditorGUILayout.Slider(
                new GUIContent(
                    I18N.T("RequestTimeoutSec"),
                    I18N.T("RequestTimeoutDescription")
                ),
                LLMAIConfig.Instance.timeoutSeconds,
                5f,
                120f
            );

            bool newEnablePromptLogs = EditorGUILayout.Toggle(
                new GUIContent(
                    I18N.T("EnablePromptLogs"),
                    I18N.T("EnablePromptLogs")
                ),
                LLMAIConfig.Instance.enablePromptLogs
            );

            if (EditorGUI.EndChangeCheck())
            {
                LLMAIConfig.Instance.apiUrl = newApiUrl;
                LLMAIConfig.Instance.apiKey = newApiKey;
                LLMAIConfig.Instance.modelName = newModelName;
                LLMAIConfig.Instance.useMaxCompletionTokens = newUseMaxCompletionTokens;
                LLMAIConfig.Instance.temperature = newTemperature;
                LLMAIConfig.Instance.maxConcurrentRequests = newMaxConcurrentRequests;
                LLMAIConfig.Instance.maxRetries = newMaxRetries;
                LLMAIConfig.Instance.retryDelaySeconds = newRetryDelay;
                LLMAIConfig.Instance.timeoutSeconds = newTimeout;
                LLMAIConfig.Instance.enablePromptLogs = newEnablePromptLogs;
                LLMAIConfig.Instance.SaveChanges();
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox(I18N.T("APIConfigHelpText"), MessageType.Info);

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField(I18N.T("PromptSettings"), EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(I18N.T("OpenPromptSettings"), GUILayout.ExpandWidth(true)))
            {
                PromptSettingsWindow.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private async void TestConnection()
        {
            EditorUtility.DisplayProgressBar(I18N.T("TestingConnection"), I18N.T("TestingConnection"), 0.5f);
            try
            {
                string result = await LLMAIService.Instance.TranslateText("测试连接", "English", "test_connection");
                if(!string.IsNullOrEmpty(result))
                {
                    EditorUtility.DisplayDialog(I18N.T("Success"), I18N.T("ConnectionTestSuccess"), I18N.T("OK"));
                }
            }
            catch(Exception e)
            {
                EditorUtility.DisplayDialog(I18N.T("Error"), string.Format(I18N.T("ConnectionTestFailed"), e.Message), I18N.T("OK"));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Focus();
            }
        }
    }
}