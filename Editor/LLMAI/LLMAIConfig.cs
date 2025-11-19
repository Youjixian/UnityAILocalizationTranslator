using UnityEngine;
using System;
using UnityEditor;
using System.IO;

namespace CardGame.Editor.LLMAI
{
    [Serializable]
    public class LLMAIConfig : ScriptableObject
    {
        private static string CONFIG_PATH
        {
            get
            {
                // 将配置文件存储在 Editor 目录下，确保不会被打包
                return "Assets/Editor/LLMAI/AIConfig.asset";
            }
        }
        
        private static LLMAIConfig instance;
        public static LLMAIConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = AssetDatabase.LoadAssetAtPath<LLMAIConfig>(CONFIG_PATH);
                    if (instance == null)
                    {
                        instance = CreateInstance<LLMAIConfig>();
                        #if UNITY_EDITOR
                        // 确保 Editor/LLMAI 目录存在
                        if (!AssetDatabase.IsValidFolder("Assets/Editor/LLMAI"))
                        {
                            // 确认 Editor 目录存在
                            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                            {
                                AssetDatabase.CreateFolder("Assets", "Editor");
                                AssetDatabase.Refresh();
                            }
                            
                            AssetDatabase.CreateFolder("Assets/Editor", "LLMAI");
                            AssetDatabase.Refresh();
                        }
                        
                        AssetDatabase.CreateAsset(instance, CONFIG_PATH);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        #endif
                    }
                }
                return instance;
            }
        }

        // 基本API配置
        public string apiUrl = "https://api.deepseek.com/v1/chat/completions";
        public string apiKey = "";
        public string modelName = "deepseek-chat";
        public bool useMaxCompletionTokens = false;
        public float temperature = 0.7f;
        public bool useDefaultTemperature = false;
        
        // 性能设置
        public int maxConcurrentRequests = 3;
        public int maxRetries = 3;
        public float retryDelaySeconds = 1f;
        public float timeoutSeconds = 30f;

        // 提示词模板
        [TextArea(3, 10)]
        public string translationSystemPromptTemplate = "";
        [TextArea(3, 10)]
        public string reviewSystemPromptTemplate = "";
        [Obsolete]
        [TextArea(3, 10)]
        public string fixSystemPromptTemplate = "";

        public bool useLanguageSupplementPrompts = false;
        public bool enablePromptLogs = false;

        public void SaveChanges()
        {
            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            #endif
        }
    }
}