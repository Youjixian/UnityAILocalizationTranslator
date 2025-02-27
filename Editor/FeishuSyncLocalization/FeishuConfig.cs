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
            // 将配置文件存储在 Editor 目录下，确保不会被打包
            return "Assets/Editor/FeishuSync/FeishuConfig.asset";
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
                    // 确保 Editor/FeishuSync 目录存在
                    if (!AssetDatabase.IsValidFolder("Assets/Editor/FeishuSync"))
                    {
                        // 确认 Editor 目录存在
                        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                        {
                            AssetDatabase.CreateFolder("Assets", "Editor");
                            AssetDatabase.Refresh();
                        }
                        
                        AssetDatabase.CreateFolder("Assets/Editor", "FeishuSync");
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