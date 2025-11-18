using UnityEngine;
using UnityEditor;
using UnityEditor.Localization;
using System.Linq;
using CardGame.Editor;

namespace CardGame.Editor.LLMAI
{
    public class PromptSettingsWindow : EditorWindow
    {
        Vector2 scroll;

        [MenuItem("Tools/Localization/AI Prompt Settings")]
        public static void ShowWindow()
        {
            var w = GetWindow<PromptSettingsWindow>(I18N.T("WindowTitlePromptSettings"));
            w.minSize = new Vector2(500, 400);
        }

        void OnGUI()
        {
            var cfg = LLMAIConfig.Instance;
            var lcfg = LanguagePromptConfig.Instance;

            EditorGUILayout.LabelField(I18N.T("Prompt_GlobalSettings"), EditorStyles.boldLabel);
            cfg.useLanguageSupplementPrompts = EditorGUILayout.Toggle(I18N.T("Prompt_EnableSupplements"), cfg.useLanguageSupplementPrompts);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(I18N.T("Prompt_SystemTemplates"), EditorStyles.boldLabel);
            var style = new GUIStyle(EditorStyles.textArea);
            style.wordWrap = true;
            float width = position.width - 40f;

            EditorGUILayout.LabelField(I18N.T("Prompt_TranslationTemplate"));
            var transContent = new GUIContent(string.IsNullOrEmpty(cfg.translationSystemPromptTemplate) ? I18N.T("DefaultTranslationPrompt") : cfg.translationSystemPromptTemplate);
            float transHeight = style.CalcHeight(transContent, width);
            cfg.translationSystemPromptTemplate = EditorGUILayout.TextArea(transContent.text, style, GUILayout.Height(Mathf.Max(60f, Mathf.Min(transHeight, 400f))), GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField(I18N.T("Prompt_ReviewTemplate"));
            var reviewContent = new GUIContent(string.IsNullOrEmpty(cfg.reviewSystemPromptTemplate) ? I18N.T("DefaultReviewPrompt") : cfg.reviewSystemPromptTemplate);
            float reviewHeight = style.CalcHeight(reviewContent, width);
            cfg.reviewSystemPromptTemplate = EditorGUILayout.TextArea(reviewContent.text, style, GUILayout.Height(Mathf.Max(60f, Mathf.Min(reviewHeight, 400f))), GUILayout.ExpandWidth(true));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(I18N.T("Prompt_PerLanguageSupplements"), EditorStyles.boldLabel);
            using (var scrollView = new GUILayout.ScrollViewScope(scroll))
            {
                scroll = scrollView.scrollPosition;
                var locales = LocalizationEditorSettings.GetLocales();
                foreach (var locale in locales)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField(locale.LocaleName + " (" + locale.Identifier.Code + ")", EditorStyles.boldLabel);
                    var profile = lcfg.GetProfile(locale.Identifier.Code) ?? new LanguagePromptEntry { langCode = locale.Identifier.Code };
                    EditorGUILayout.LabelField(I18N.T("Prompt_TranslationSupplement"));
                    var tContent = new GUIContent(profile.translationSupplement ?? string.Empty);
                    float tHeight = style.CalcHeight(tContent, width);
                    profile.translationSupplement = EditorGUILayout.TextArea(tContent.text, style, GUILayout.Height(Mathf.Max(60f, Mathf.Min(tHeight, 400f))), GUILayout.ExpandWidth(true));
                    EditorGUILayout.LabelField(I18N.T("Prompt_ReviewSupplement"));
                    var rContent = new GUIContent(profile.reviewSupplement ?? string.Empty);
                    float rHeight = style.CalcHeight(rContent, width);
                    profile.reviewSupplement = EditorGUILayout.TextArea(rContent.text, style, GUILayout.Height(Mathf.Max(60f, Mathf.Min(rHeight, 400f))), GUILayout.ExpandWidth(true));
                    profile.notes = EditorGUILayout.TextField(I18N.T("Prompt_Notes"), profile.notes);
                    if (GUILayout.Button(I18N.T("Save") + " " + locale.Identifier.Code))
                    {
                        lcfg.SetProfile(profile);
                    }
                    EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField("––––––––––––––––––––––––––––");
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(I18N.T("Prompt_ExportJSON")))
            {
                var path = EditorUtility.SaveFilePanel("Export Language Prompts", Application.dataPath, "LanguagePrompts", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    var json = lcfg.ExportToJson();
                    System.IO.File.WriteAllText(path, json);
                    AssetDatabase.Refresh();
                }
            }
            if (GUILayout.Button(I18N.T("Prompt_ImportJSON")))
            {
                var path = EditorUtility.OpenFilePanel("Import Language Prompts", Application.dataPath, "json");
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    var json = System.IO.File.ReadAllText(path);
                    lcfg.ImportFromJson(json);
                    Repaint();
                }
            }
            if (GUILayout.Button(I18N.T("Prompt_OpenAISettings")))
            {
                LLMAIConfigWindow.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
            {
                cfg.SaveChanges();
            }
        }
    }
}