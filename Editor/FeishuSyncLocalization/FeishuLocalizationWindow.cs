using UnityEngine;
using UnityEditor;
using UnityEditor.Localization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using CardGame.Editor;

public class FeishuLocalizationWindow : EditorWindow
{
    private Vector2 _scrollPosition;
    private Dictionary<string, string> _tableNames = new Dictionary<string, string>();
    private FeishuService _feishuService;
    private LocalizationSyncManager _syncManager;
    private bool _isSyncing;
    private string _syncStatus = "";
    private bool _showAdvancedOptions;

    [MenuItem("Tools/Localization/Feishu Sync")]
    public static void ShowWindow()
    {
        var window = GetWindow<FeishuLocalizationWindow>();
        window.titleContent = new GUIContent(I18N.T("WindowTitleFeishu"));
        window.Show();
    }

    private void OnEnable()
    {
        _feishuService = new FeishuService();
        _syncManager = new LocalizationSyncManager();
    }

    private async void LoadTables()
    {
        try
        {
            _isSyncing = true;
            _syncStatus = I18N.T("LoadingTableList");
            var tables = await _feishuService.ListTables();
            _tableNames.Clear();
            
            foreach (var table in tables)
            {
                string tableId = table["table_id"].ToString();
                string tableName = table["name"].ToString();
                _tableNames[tableId] = tableName;
            }
            Repaint();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载表格失败: {e.Message}");
        }
        finally
        {
            _isSyncing = false;
            _syncStatus = "";
            Repaint();
        }
    }

    private void OnGUI()
    {
        // 绘制标题
        GUILayout.Label(I18N.T("WindowTitleFeishu"), EditorStyles.boldLabel);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        EditorGUILayout.BeginVertical();

        DrawLocalizationTablesSection();
        EditorGUILayout.Space();
        DrawSyncSection();
        EditorGUILayout.Space();
        DrawAdvancedSection();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();

        if (_isSyncing)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField(I18N.T("SyncStatus"), _syncStatus);
            EditorGUI.EndDisabledGroup();
        }
    }

    private void DrawLocalizationTablesSection()
    {
        EditorGUILayout.LabelField(I18N.T("LocalizationTablesSection"), EditorStyles.boldLabel);

        var stringTables = LocalizationEditorSettings.GetStringTableCollections();
        if (stringTables.Count == 0)
        {
            EditorGUILayout.HelpBox(I18N.T("NoLocalizationTablesWarning"), MessageType.Warning);
            return;
        }

        EditorGUI.BeginDisabledGroup(true);
        foreach (var table in stringTables)
        {
            EditorGUILayout.TextField(I18N.T("TableName"), table.TableCollectionName);
        }
        EditorGUI.EndDisabledGroup();
    }

    private void DrawSyncSection()
    {
        EditorGUILayout.LabelField(I18N.T("SyncOperations"), EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(_isSyncing);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(I18N.T("PushToFeishu")))
        {
            PushToFeishu();
        }

        if (GUILayout.Button(I18N.T("PullFromFeishu")))
        {
            PullFromFeishu();
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button(I18N.T("SyncWithFeishu")))
        {
            SyncWithFeishu();
        }

        EditorGUI.EndDisabledGroup();
    }

    private void DrawAdvancedSection()
    {
        _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, I18N.T("AdvancedOptions"));
        if (_showAdvancedOptions)
        {
            EditorGUI.indentLevel++;
            
            if (GUILayout.Button(I18N.T("ReloadLocalizations")))
            {
                ReloadLocalizations();
            }

            EditorGUI.indentLevel--;
        }
    }

    private async void TestConnection()
    {
        try
        {
            _isSyncing = true;
            _syncStatus = I18N.T("TestingConnection");
            await _feishuService.GetAccessToken();
            EditorUtility.DisplayDialog(I18N.T("Success"), I18N.T("ConnectionTestSuccess"), I18N.T("OK"));
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog(I18N.T("Error"), string.Format(I18N.T("ConnectionTestFailed"), e.Message), I18N.T("OK"));
        }
        finally
        {
            _isSyncing = false;
            _syncStatus = "";
            Repaint();
        }
    }

    private async void CreateNewTable()
    {
        try
        {
            _isSyncing = true;
            _syncStatus = I18N.T("CreatingNewTable");
            
            var result = await _feishuService.CreateTable("Unity本地化");
            if (result["code"].Value<int>() == 0)
            {
                EditorUtility.DisplayDialog(I18N.T("Success"), I18N.T("CreateTableSuccess"), I18N.T("OK"));
                LoadTables();
            }
            else
            {
                EditorUtility.DisplayDialog(I18N.T("Error"), string.Format(I18N.T("CreateTableFailed"), result["msg"]), I18N.T("OK"));
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog(I18N.T("Error"), string.Format(I18N.T("CreateTableFailed"), e.Message), I18N.T("OK"));
        }
        finally
        {
            _isSyncing = false;
            _syncStatus = "";
            Repaint();
        }
    }

    private async void PushToFeishu()
    {
        try
        {
            _isSyncing = true;
            _syncStatus = I18N.T("LoadingLocalizations");
            await _syncManager.LoadLocalizations();

            _syncStatus = I18N.T("PushingToFeishu");
            await _syncManager.PushToFeishu();

            EditorUtility.DisplayDialog(I18N.T("Success"), I18N.T("PushSuccess"), I18N.T("OK"));
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog(I18N.T("Error"), string.Format(I18N.T("PushFailed"), e.Message), I18N.T("OK"));
        }
        finally
        {
            _isSyncing = false;
            _syncStatus = "";
            Repaint();
        }
    }

    private async void PullFromFeishu()
    {
        try
        {
            _isSyncing = true;
            _syncStatus = I18N.T("PullingFromFeishu");
            await _syncManager.PullFromFeishu();

            EditorUtility.DisplayDialog(I18N.T("Success"), I18N.T("PullSuccess"), I18N.T("OK"));
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog(I18N.T("Error"), string.Format(I18N.T("PullFailed"), e.Message), I18N.T("OK"));
        }
        finally
        {
            _isSyncing = false;
            _syncStatus = "";
            Repaint();
        }
    }

    private async void SyncWithFeishu()
    {
        try
        {
            _isSyncing = true;
            _syncStatus = I18N.T("LoadingLocalizations");
            await _syncManager.LoadLocalizations();

            _syncStatus = I18N.T("SyncingData");
            await _syncManager.SyncWithFeishu();

            EditorUtility.DisplayDialog(I18N.T("Success"), I18N.T("SyncSuccess"), I18N.T("OK"));
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog(I18N.T("Error"), string.Format(I18N.T("SyncFailed"), e.Message), I18N.T("OK"));
        }
        finally
        {
            _isSyncing = false;
            _syncStatus = "";
            Repaint();
        }
    }

    private async void ReloadLocalizations()
    {
        try
        {
            _isSyncing = true;
            _syncStatus = I18N.T("ReloadingLocalizations");
            await _syncManager.LoadLocalizations();
            EditorUtility.DisplayDialog(I18N.T("Success"), I18N.T("ReloadSuccess"), I18N.T("OK"));
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog(I18N.T("Error"), string.Format(I18N.T("ReloadFailed"), e.Message), I18N.T("OK"));
        }
        finally
        {
            _isSyncing = false;
            _syncStatus = "";
            Repaint();
        }
    }
} 