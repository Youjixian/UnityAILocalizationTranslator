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
                // 获取当前脚本所在目录
                MonoScript script = MonoScript.FromScriptableObject(CreateInstance<LLMAIConfig>());
                string scriptPath = AssetDatabase.GetAssetPath(script);
                string directory = Path.GetDirectoryName(scriptPath);
                // 返回相对于脚本的配置文件路径
                return Path.Combine(directory, "AIConfig.asset");
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
                        string directory = System.IO.Path.GetDirectoryName(CONFIG_PATH);
                        if (!System.IO.Directory.Exists(directory))
                        {
                            System.IO.Directory.CreateDirectory(directory);
                        }
                        AssetDatabase.CreateAsset(instance, CONFIG_PATH);
                        AssetDatabase.SaveAssets();
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
        
        // 性能设置
        public int maxConcurrentRequests = 3;
        public int maxRetries = 3;
        public float retryDelaySeconds = 1f;
        public float timeoutSeconds = 30f;

        public void SaveChanges()
        {
            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            #endif
        }
    }
} 