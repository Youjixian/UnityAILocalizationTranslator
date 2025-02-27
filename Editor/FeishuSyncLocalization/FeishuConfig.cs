using UnityEngine;
using System;
using UnityEditor;
using System.IO;

[Serializable]
public class FeishuConfig : ScriptableObject
{
    private static string CONFIG_PATH
    {
        get
        {
            // 获取当前脚本所在目录
            MonoScript script = MonoScript.FromScriptableObject(CreateInstance<FeishuConfig>());
            string scriptPath = AssetDatabase.GetAssetPath(script);
            string directory = Path.GetDirectoryName(scriptPath);
            // 返回相对于脚本的配置文件路径
            return Path.Combine(directory, "FeishuConfig.asset");
        }
    }
    
    private static FeishuConfig instance;
    public static FeishuConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = AssetDatabase.LoadAssetAtPath<FeishuConfig>(CONFIG_PATH);
                if (instance == null)
                {
                    instance = CreateInstance<FeishuConfig>();
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

    // 基本配置
    public string AppId = "";
    public string AppSecret = "";
    public string TableId = "";

    public void SaveChanges()
    {
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        #endif
    }
} 