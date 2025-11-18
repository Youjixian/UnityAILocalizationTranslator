using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using System.Linq;

namespace CardGame.Editor.LLMAI
{
    [Serializable]
    public class LanguagePromptEntry
    {
        public string langCode;
        public string translationSupplement;
        public string reviewSupplement;
        public string notes;
    }

    [Serializable]
    class LanguagePromptWrapper
    {
        public List<LanguagePromptEntry> entries;
    }

    public class LanguagePromptConfig : ScriptableObject
    {
        static string CONFIG_PATH => "Assets/Editor/LLMAI/LanguagePrompts.asset";

        static LanguagePromptConfig instance;
        public static LanguagePromptConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = AssetDatabase.LoadAssetAtPath<LanguagePromptConfig>(CONFIG_PATH);
                    if (instance == null)
                    {
                        instance = CreateInstance<LanguagePromptConfig>();
                        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                        {
                            AssetDatabase.CreateFolder("Assets", "Editor");
                            AssetDatabase.Refresh();
                        }
                        if (!AssetDatabase.IsValidFolder("Assets/Editor/LLMAI"))
                        {
                            AssetDatabase.CreateFolder("Assets/Editor", "LLMAI");
                            AssetDatabase.Refresh();
                        }
                        AssetDatabase.CreateAsset(instance, CONFIG_PATH);
                        // 预填充当前项目的语言条目，避免首次保存丢失
                        var locales = LocalizationEditorSettings.GetLocales();
                        foreach (var locale in locales)
                        {
                            instance.entries.Add(new LanguagePromptEntry
                            {
                                langCode = locale.Identifier.Code,
                                translationSupplement = string.Empty,
                                reviewSupplement = string.Empty,
                                notes = string.Empty
                            });
                        }
                        EditorUtility.SetDirty(instance);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
                return instance;
            }
        }

        public List<LanguagePromptEntry> entries = new List<LanguagePromptEntry>();

        public void SaveChanges()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public LanguagePromptEntry GetProfile(string langCode)
        {
            if (string.IsNullOrEmpty(langCode)) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (!string.IsNullOrEmpty(e.langCode) && string.Equals(e.langCode, langCode, StringComparison.OrdinalIgnoreCase))
                {
                    return e;
                }
            }
            return null;
        }

        public void SetProfile(LanguagePromptEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.langCode)) return;
            var existing = GetProfile(entry.langCode);
            if (existing == null)
            {
                entries.Add(entry);
            }
            else
            {
                existing.translationSupplement = entry.translationSupplement;
                existing.reviewSupplement = entry.reviewSupplement;
                existing.notes = entry.notes;
            }
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public string ExportToJson()
        {
            var wrapper = new LanguagePromptWrapper { entries = entries };
            return JsonUtility.ToJson(wrapper, true);
        }

        public void ImportFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            var wrapper = JsonUtility.FromJson<LanguagePromptWrapper>(json);
            if (wrapper != null && wrapper.entries != null)
            {
                entries = new List<LanguagePromptEntry>(wrapper.entries);
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }
    }
}