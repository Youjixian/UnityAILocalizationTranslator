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

            if (EditorGUI.EndChangeCheck())
            {
                LLMAIConfig.Instance.apiUrl = newApiUrl;
                LLMAIConfig.Instance.apiKey = newApiKey;
                LLMAIConfig.Instance.modelName = newModelName;
                LLMAIConfig.Instance.maxConcurrentRequests = newMaxConcurrentRequests;
                LLMAIConfig.Instance.maxRetries = newMaxRetries;
                LLMAIConfig.Instance.retryDelaySeconds = newRetryDelay;
                LLMAIConfig.Instance.timeoutSeconds = newTimeout;
                LLMAIConfig.Instance.SaveChanges();
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox(I18N.T("APIConfigHelpText"), MessageType.Info);

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField(I18N.T("PromptTemplates"), EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (promptTextAreaStyle == null)
            {
                promptTextAreaStyle = new GUIStyle(EditorStyles.textArea);
                promptTextAreaStyle.wordWrap = true;
            }

            EditorGUI.BeginChangeCheck();
            // Translation prompt
            EditorGUILayout.LabelField(I18N.T("TranslationPromptTemplate"));
            var transTplInitial = string.IsNullOrEmpty(LLMAIConfig.Instance.translationSystemPromptTemplate)
                ? I18N.T("DefaultTranslationPrompt")
                : LLMAIConfig.Instance.translationSystemPromptTemplate;
            float calcWidth1 = position.width - 40f;
            float calcHeight1 = promptTextAreaStyle.CalcHeight(new GUIContent(transTplInitial), calcWidth1);
            var transTpl = GUILayout.TextArea(transTplInitial, promptTextAreaStyle, GUILayout.Height(Mathf.Max(80f, Mathf.Min(calcHeight1, 400f))), GUILayout.ExpandWidth(true));

            // Review prompt
            EditorGUILayout.LabelField(I18N.T("ReviewPromptTemplate"));
            var reviewTplInitial = string.IsNullOrEmpty(LLMAIConfig.Instance.reviewSystemPromptTemplate)
                ? I18N.T("DefaultReviewPrompt")
                : LLMAIConfig.Instance.reviewSystemPromptTemplate;
            float calcWidth2 = position.width - 40f;
            float calcHeight2 = promptTextAreaStyle.CalcHeight(new GUIContent(reviewTplInitial), calcWidth2);
            var reviewTpl = GUILayout.TextArea(reviewTplInitial, promptTextAreaStyle, GUILayout.Height(Mathf.Max(80f, Mathf.Min(calcHeight2, 400f))), GUILayout.ExpandWidth(true));

            // Fix prompt
            EditorGUILayout.LabelField(I18N.T("FixPromptTemplate"));
            var fixTplInitial = string.IsNullOrEmpty(LLMAIConfig.Instance.fixSystemPromptTemplate)
                ? I18N.T("DefaultFixPrompt")
                : LLMAIConfig.Instance.fixSystemPromptTemplate;
            float calcWidth3 = position.width - 40f;
            float calcHeight3 = promptTextAreaStyle.CalcHeight(new GUIContent(fixTplInitial), calcWidth3);
            var fixTpl = GUILayout.TextArea(fixTplInitial, promptTextAreaStyle, GUILayout.Height(Mathf.Max(80f, Mathf.Min(calcHeight3, 400f))), GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                LLMAIConfig.Instance.translationSystemPromptTemplate = transTpl;
                LLMAIConfig.Instance.reviewSystemPromptTemplate = reviewTpl;
                LLMAIConfig.Instance.fixSystemPromptTemplate = fixTpl;
                LLMAIConfig.Instance.SaveChanges();
            }

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