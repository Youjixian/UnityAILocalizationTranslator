using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CardGame.Editor
{
    /// <summary>
    /// 集中管理语言设置的编辑器窗口
    /// </summary>
    public class LanguageSettings : EditorWindow
    {
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Localization/Language Settings", false, 1)]
        public static void ShowWindow()
        {
            // 获取或创建窗口
            LanguageSettings window = GetWindow<LanguageSettings>(false, I18N.T("WindowTitleSettings"), true);
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        private void OnEnable()
        {
            // 初始化样式
            headerStyle = new GUIStyle();
            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.margin = new RectOffset(5, 5, 10, 10);
            headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            subHeaderStyle = new GUIStyle();
            subHeaderStyle.fontSize = 14;
            subHeaderStyle.fontStyle = FontStyle.Bold;
            subHeaderStyle.margin = new RectOffset(5, 5, 5, 5);
            subHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 标题
            EditorGUILayout.LabelField(I18N.T("LanguageSettings"), headerStyle);
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(I18N.T("SelectLanguage"), subHeaderStyle);
            EditorGUILayout.Space(5);

            // 语言选择
            EditorGUI.BeginChangeCheck();
            I18N.Language selectedLanguage = (I18N.Language)EditorGUILayout.EnumPopup(I18N.CurrentLanguage);
            if (EditorGUI.EndChangeCheck())
            {
                I18N.CurrentLanguage = selectedLanguage;
                // 语言变更后重新绘制窗口
                Repaint();
            }
            
            EditorGUILayout.Space(5);
            
            // 说明文本
            string description = I18N.CurrentLanguage == I18N.Language.Chinese 
                ? "此设置将应用于所有本地化插件窗口，无需在各窗口单独设置" 
                : "This setting will apply to all localization plugin windows, no need to set in each window separately";
            
            EditorGUILayout.HelpBox(description, MessageType.Info);
            
            EditorGUILayout.EndVertical();

            // 添加一些空间
            EditorGUILayout.Space(20);

            // 显示当前翻译状态
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string status = I18N.CurrentLanguage == I18N.Language.Chinese 
                ? "当前界面语言：中文" 
                : "Current Interface Language: English";
            EditorGUILayout.LabelField(status, EditorStyles.boldLabel);

            // 显示插件信息
            string versionInfo = I18N.CurrentLanguage == I18N.Language.Chinese
                ? "本地化工具 v0.0.1" 
                : "Localization Tools v0.0.1";
            EditorGUILayout.LabelField(versionInfo);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }
    }
} 