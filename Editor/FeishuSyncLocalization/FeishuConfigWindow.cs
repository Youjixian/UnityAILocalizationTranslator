using UnityEngine;
using UnityEditor;
using System;
using CardGame.Editor;

public class FeishuConfigWindow : EditorWindow
{
    private Vector2 scrollPosition;

    [MenuItem("Tools/Localization/Feishu API Settings")]
    public static void ShowWindow()
    {
        var window = GetWindow<FeishuConfigWindow>(I18N.T("FeishuAPISettings"));
        window.minSize = new Vector2(400, 250);
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(I18N.T("FeishuAPISettings"), EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUI.BeginChangeCheck();

        string newAppId = EditorGUILayout.TextField(I18N.T("AppID"), FeishuConfig.Instance.AppId);
        
        EditorGUILayout.BeginHorizontal();
        string newAppSecret = EditorGUILayout.PasswordField(I18N.T("AppSecret"), FeishuConfig.Instance.AppSecret);
        if(GUILayout.Button(I18N.T("TestConnection"), GUILayout.Width(80)))
        {
            TestConnection();
        }
        EditorGUILayout.EndHorizontal();

        string newTableId = EditorGUILayout.TextField(I18N.T("TableDefinition"), FeishuConfig.Instance.TableId);
        
        if (EditorGUI.EndChangeCheck())
        {
            FeishuConfig.Instance.AppId = newAppId;
            FeishuConfig.Instance.AppSecret = newAppSecret;
            FeishuConfig.Instance.TableId = newTableId;
            FeishuConfig.Instance.SaveChanges();
        }

        EditorGUILayout.Space(20);
        string helpText = I18N.CurrentLanguage == I18N.Language.Chinese ?
            "此配置将用于飞书API连接和表格同步。\n请确保App ID和App Secret已正确设置。\n如需创建飞书应用，请访问飞书开放平台。" :
            "This configuration will be used for Feishu API connection and table synchronization.\nPlease ensure App ID and App Secret are set correctly.\nVisit Feishu Open Platform to create a Feishu application.";
        
        EditorGUILayout.HelpBox(helpText, MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private async void TestConnection()
    {
        EditorUtility.DisplayProgressBar("测试连接", "正在测试飞书API连接...", 0.5f);
        try
        {
            FeishuService feishuService = new FeishuService();
            var tables = await feishuService.ListTables();
            if(tables != null)
            {
                EditorUtility.DisplayDialog("成功", "飞书API连接测试成功！", "确定");
            }
        }
        catch(Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"飞书API连接测试失败：{e.Message}", "确定");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
} 