using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CardGame.Editor
{
    /// <summary>
    /// 插件本地化管理类，支持中英文切换
    /// </summary>
    public static class I18N
    {
        // 语言枚举
        public enum Language
        {
            English,
            Chinese
        }

        // 当前语言
        private static Language _currentLanguage = Language.Chinese;

        // 获取或设置当前语言
        public static Language CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    // 保存语言设置到EditorPrefs
                    EditorPrefs.SetInt("LocalizationTranslator_Language", (int)value);
                    // 刷新所有编辑器窗口
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
            }
        }

        // 翻译字典
        private static readonly Dictionary<string, Dictionary<Language, string>> _translations = new Dictionary<string, Dictionary<Language, string>>()
        {
            // 公共
            {"LanguageSwitcher", new Dictionary<Language, string>
                {
                    {Language.English, "Language"},
                    {Language.Chinese, "语言"}
                }
            },
            {"Save", new Dictionary<Language, string>
                {
                    {Language.English, "Save"},
                    {Language.Chinese, "保存"}
                }
            },
            {"Cancel", new Dictionary<Language, string>
                {
                    {Language.English, "Cancel"},
                    {Language.Chinese, "取消"}
                }
            },
            {"Apply", new Dictionary<Language, string>
                {
                    {Language.English, "Apply"},
                    {Language.Chinese, "应用"}
                }
            },
            {"OK", new Dictionary<Language, string>
                {
                    {Language.English, "OK"},
                    {Language.Chinese, "确定"}
                }
            },
            {"Close", new Dictionary<Language, string>
                {
                    {Language.English, "Close"},
                    {Language.Chinese, "关闭"}
                }
            },

            // 菜单项
            {"MenuPathLLMAI", new Dictionary<Language, string>
                {
                    {Language.English, "Tools/Localization/AI Translator"},
                    {Language.Chinese, "游际线/本地化翻译工具"}
                }
            },
            {"MenuPathFeishu", new Dictionary<Language, string>
                {
                    {Language.English, "Tools/Localization/Feishu Sync"},
                    {Language.Chinese, "游际线/飞书本地化同步工具"}
                }
            },
            {"MenuPathSettings", new Dictionary<Language, string>
                {
                    {Language.English, "Tools/Localization/Language Settings"},
                    {Language.Chinese, "游际线/本地化语言设置"}
                }
            },

            // 窗口标题
            {"WindowTitleLLMAI", new Dictionary<Language, string>
                {
                    {Language.English, "AI Translator"},
                    {Language.Chinese, "本地化翻译工具"}
                }
            },
            {"WindowTitleFeishu", new Dictionary<Language, string>
                {
                    {Language.English, "Feishu Localization Sync"},
                    {Language.Chinese, "飞书本地化同步"}
                }
            },
            {"WindowTitleSettings", new Dictionary<Language, string>
                {
                    {Language.English, "Language Settings"},
                    {Language.Chinese, "语言设置"}
                }
            },

            // 语言设置窗口
            {"LanguageSettings", new Dictionary<Language, string>
                {
                    {Language.English, "Language Settings"},
                    {Language.Chinese, "语言设置"}
                }
            },
            {"SelectLanguage", new Dictionary<Language, string>
                {
                    {Language.English, "Select Interface Language"},
                    {Language.Chinese, "选择界面语言"}
                }
            },

            // LLMAI窗口
            {"SourceLanguage", new Dictionary<Language, string>
                {
                    {Language.English, "Source Language"},
                    {Language.Chinese, "源语言"}
                }
            },
            {"TargetLanguages", new Dictionary<Language, string>
                {
                    {Language.English, "Target Languages"},
                    {Language.Chinese, "目标语言"}
                }
            },
            {"SelectCollection", new Dictionary<Language, string>
                {
                    {Language.English, "Select Collection"},
                    {Language.Chinese, "选择字符串表格"}
                }
            },
            {"AdvancedOptions", new Dictionary<Language, string>
                {
                    {Language.English, "Advanced Options"},
                    {Language.Chinese, "高级选项"}
                }
            },
            {"TranslateEmptyOnly", new Dictionary<Language, string>
                {
                    {Language.English, "Translate Empty Only"},
                    {Language.Chinese, "仅翻译空白条目"}
                }
            },
            {"IncludeDesc", new Dictionary<Language, string>
                {
                    {Language.English, "Include Description"},
                    {Language.Chinese, "包含描述"}
                }
            },
            {"IncludeKey", new Dictionary<Language, string>
                {
                    {Language.English, "Include Key"},
                    {Language.Chinese, "包含键名"}
                }
            },
            {"StartTranslation", new Dictionary<Language, string>
                {
                    {Language.English, "Start Translation"},
                    {Language.Chinese, "开始翻译"}
                }
            },
            {"StopTranslation", new Dictionary<Language, string>
                {
                    {Language.English, "Stop Translation"},
                    {Language.Chinese, "停止翻译"}
                }
            },
            {"PauseTranslation", new Dictionary<Language, string>
                {
                    {Language.English, "Pause Translation"},
                    {Language.Chinese, "暂停翻译"}
                }
            },
            {"ResumeTranslation", new Dictionary<Language, string>
                {
                    {Language.English, "Resume Translation"},
                    {Language.Chinese, "继续翻译"}
                }
            },
            {"TranslationOptions", new Dictionary<Language, string>
                {
                    {Language.English, "Translation Options"},
                    {Language.Chinese, "翻译选项"}
                }
            },
            {"BasicSettings", new Dictionary<Language, string>
                {
                    {Language.English, "Basic Settings"},
                    {Language.Chinese, "基本设置"}
                }
            },
            {"AISettings", new Dictionary<Language, string>
                {
                    {Language.English, "AI Settings"},
                    {Language.Chinese, "AI设置"}
                }
            },
            {"OpenAISettings", new Dictionary<Language, string>
                {
                    {Language.English, "OpenAI Settings"},
                    {Language.Chinese, "OpenAI设置"}
                }
            },
            {"APIKey", new Dictionary<Language, string>
                {
                    {Language.English, "API Key"},
                    {Language.Chinese, "API Key"}
                }
            },
            {"APIURL", new Dictionary<Language, string>
                {
                    {Language.English, "API URL"},
                    {Language.Chinese, "API URL"}
                }
            },
            {"ModelName", new Dictionary<Language, string>
                {
                    {Language.English, "Model Name"},
                    {Language.Chinese, "模型名称"}
                }
            },
            {"MaxTokens", new Dictionary<Language, string>
                {
                    {Language.English, "Max Tokens"},
                    {Language.Chinese, "最大开放字数"}
                }
            },
            {"MaxConcurrentRequests", new Dictionary<Language, string>
                {
                    {Language.English, "Max Concurrent Requests"},
                    {Language.Chinese, "最大并发次数"}
                }
            },
            {"MaxRetries", new Dictionary<Language, string>
                {
                    {Language.English, "Max Retries"},
                    {Language.Chinese, "重试总次数(次)"}
                }
            },
            {"RequestTimeout", new Dictionary<Language, string>
                {
                    {Language.English, "Request Timeout (s)"},
                    {Language.Chinese, "请求超时(秒)"}
                }
            },
            {"CarryLocalKey", new Dictionary<Language, string>
                {
                    {Language.English, "Include LocalKey"},
                    {Language.Chinese, "携带本地化Key"}
                }
            },
            {"SourceLang", new Dictionary<Language, string>
                {
                    {Language.English, "Source Language"},
                    {Language.Chinese, "源语言"}
                }
            },
            {"TranslationPrecision", new Dictionary<Language, string>
                {
                    {Language.English, "Translation Precision"},
                    {Language.Chinese, "携带地区高翻译精准度"}
                }
            },
            {"AutoTranslate", new Dictionary<Language, string>
                {
                    {Language.English, "Auto Translate"},
                    {Language.Chinese, "自动语言翻译"}
                }
            },

            // 飞书同步窗口
            {"SyncSettings", new Dictionary<Language, string>
                {
                    {Language.English, "Sync Settings"},
                    {Language.Chinese, "同步设置"}
                }
            },
            {"TestConnection", new Dictionary<Language, string>
                {
                    {Language.English, "Test Connection"},
                    {Language.Chinese, "测试连接"}
                }
            },
            {"PushToFeishu", new Dictionary<Language, string>
                {
                    {Language.English, "Push to Feishu"},
                    {Language.Chinese, "推送到飞书"}
                }
            },
            {"PullFromFeishu", new Dictionary<Language, string>
                {
                    {Language.English, "Pull from Feishu"},
                    {Language.Chinese, "从飞书拉取"}
                }
            },
            {"SyncWithFeishu", new Dictionary<Language, string>
                {
                    {Language.English, "Sync with Feishu"},
                    {Language.Chinese, "与飞书同步"}
                }
            },
            {"ReloadLocalizations", new Dictionary<Language, string>
                {
                    {Language.English, "Reload Localizations"},
                    {Language.Chinese, "重新加载本地化"}
                }
            },
            {"FeishuAPISettings", new Dictionary<Language, string>
                {
                    {Language.English, "Feishu API Settings"},
                    {Language.Chinese, "飞书API设置"}
                }
            },
            {"AppID", new Dictionary<Language, string>
                {
                    {Language.English, "App ID"},
                    {Language.Chinese, "App ID"}
                }
            },
            {"AppSecret", new Dictionary<Language, string>
                {
                    {Language.English, "App Secret"},
                    {Language.Chinese, "App Secret"}
                }
            },
            {"TenantID", new Dictionary<Language, string>
                {
                    {Language.English, "Tenant ID"},
                    {Language.Chinese, "认证授权ID"}
                }
            },
            {"TableDefinition", new Dictionary<Language, string>
                {
                    {Language.English, "Table Definition"},
                    {Language.Chinese, "表格定义"}
                }
            },
            {"LocalizationTables", new Dictionary<Language, string>
                {
                    {Language.English, "Localization Tables"},
                    {Language.Chinese, "本地化表格"}
                }
            },
            
            // 补充LLMAI窗口
            {"OpenLLMSettings", new Dictionary<Language, string>
                {
                    {Language.English, "Open LLM AI Settings"},
                    {Language.Chinese, "打开LLM AI设置"}
                }
            },
            {"SelectStringTable", new Dictionary<Language, string>
                {
                    {Language.English, "Select String Table"},
                    {Language.Chinese, "选择字符串表"}
                }
            },
            {"TranslateOnlyEmpty", new Dictionary<Language, string>
                {
                    {Language.English, "Translate Empty Only"},
                    {Language.Chinese, "仅翻译空值"}
                }
            },
            {"IncludeLocalizationKey", new Dictionary<Language, string>
                {
                    {Language.English, "Include Localization Key"},
                    {Language.Chinese, "携带本地化Key"}
                }
            },
            {"KeyTooltip", new Dictionary<Language, string>
                {
                    {Language.English, "Send the localization key to AI along with the translation, providing more context to help AI better understand the purpose of the translation."},
                    {Language.Chinese, "翻译时将本地化条目的key一同发送给AI，以提供更多上下文信息，帮助AI更好地理解翻译内容的用途。"}
                }
            },
            {"IncludeDescription", new Dictionary<Language, string>
                {
                    {Language.English, "Include Description for Better Accuracy"},
                    {Language.Chinese, "携带描述提高翻译精准度"}
                }
            },
            {"DescriptionTooltip", new Dictionary<Language, string>
                {
                    {Language.English, "Send the description from Feishu to AI along with the translation, providing more context information to help AI better understand the content."},
                    {Language.Chinese, "翻译时将飞书多维表格中的描述一同发送给AI，以提供更多上下文信息，帮助AI更好地理解翻译内容。"}
                }
            },
            {"TranslateOnlyDescribed", new Dictionary<Language, string>
                {
                    {Language.English, "Translate Only Items with Description"},
                    {Language.Chinese, "仅翻译有描述的条目"}
                }
            },
            {"DescribedTooltip", new Dictionary<Language, string>
                {
                    {Language.English, "If enabled, only items with description in Feishu will be translated"},
                    {Language.Chinese, "如果启用，只会翻译飞书多维表格中有描述的条目"}
                }
            },
            {"EmptyTooltip", new Dictionary<Language, string>
                {
                    {Language.English, "If enabled, only entries that are not yet filled in the target language will be translated"},
                    {Language.Chinese, "如果启用，只会翻译目标语言中尚未填写的条目"}
                }
            },
            {"TargetLanguageSelection", new Dictionary<Language, string>
                {
                    {Language.English, "Target Language Selection"},
                    {Language.Chinese, "目标语言选择"}
                }
            },
            {"ContinueTranslation", new Dictionary<Language, string>
                {
                    {Language.English, "Continue Translation"},
                    {Language.Chinese, "继续翻译"}
                }
            },
            {"CancelTranslation", new Dictionary<Language, string>
                {
                    {Language.English, "Cancel Translation"},
                    {Language.Chinese, "取消翻译"}
                }
            },
            {"SyncStatus", new Dictionary<Language, string>
                {
                    {Language.English, "Sync Status:"},
                    {Language.Chinese, "同步状态:"}
                }
            },
            {"ConfirmTranslation", new Dictionary<Language, string>
                {
                    {Language.English, "Confirm Translation"},
                    {Language.Chinese, "确认翻译"}
                }
            },
            {"ConfirmTranslationMessage", new Dictionary<Language, string>
                {
                    {Language.English, "Will translate {0} items.\nContinue?"},
                    {Language.Chinese, "将翻译 {0} 个条目。\n是否继续？"}
                }
            },
            {"NoItemsToTranslate", new Dictionary<Language, string>
                {
                    {Language.English, "No items to translate"},
                    {Language.Chinese, "没有需要翻译的条目"}
                }
            },
            {"LoadingData", new Dictionary<Language, string>
                {
                    {Language.English, "Loading data..."},
                    {Language.Chinese, "正在加载数据..."}
                }
            },
            {"TranslatingProgress", new Dictionary<Language, string>
                {
                    {Language.English, "Translating...({0}/{1})"},
                    {Language.Chinese, "正在翻译...({0}/{1})"}
                }
            },
            {"Error", new Dictionary<Language, string>
                {
                    {Language.English, "Error"},
                    {Language.Chinese, "错误"}
                }
            },
            {"Success", new Dictionary<Language, string>
                {
                    {Language.English, "Success"},
                    {Language.Chinese, "成功"}
                }
            },
            {"Confirm", new Dictionary<Language, string>
                {
                    {Language.English, "Confirm"},
                    {Language.Chinese, "确认"}
                }
            },
            {"ConfirmContinue", new Dictionary<Language, string>
                {
                    {Language.English, "Confirm Continue"},
                    {Language.Chinese, "确认继续"}
                }
            },
            {"ConfirmContinueMessage", new Dictionary<Language, string>
                {
                    {Language.English, "Are you sure to continue translation?"},
                    {Language.Chinese, "确定要继续翻译吗？"}
                }
            },
            {"ConfirmCancel", new Dictionary<Language, string>
                {
                    {Language.English, "Confirm Cancel"},
                    {Language.Chinese, "确认取消"}
                }
            },
            {"ConfirmCancelMessage", new Dictionary<Language, string>
                {
                    {Language.English, "Are you sure to cancel the ongoing translation?"},
                    {Language.Chinese, "确定要取消正在进行的翻译吗？"}
                }
            },
            
            // LLMAI配置窗口
            {"PerformanceSettings", new Dictionary<Language, string>
                {
                    {Language.English, "Performance Settings"},
                    {Language.Chinese, "性能设置"}
                }
            },
            {"MaxRetriesCount", new Dictionary<Language, string>
                {
                    {Language.English, "Max Retries Count"},
                    {Language.Chinese, "最大重试次数"}
                }
            },
            {"MaxRetriesDescription", new Dictionary<Language, string>
                {
                    {Language.English, "Maximum number of retries when a request fails."},
                    {Language.Chinese, "当请求失败时的最大重试次数。"}
                }
            },
            {"RetryDelay", new Dictionary<Language, string>
                {
                    {Language.English, "Retry Delay (s)"},
                    {Language.Chinese, "重试延迟(秒)"}
                }
            },
            {"RetryDelayDescription", new Dictionary<Language, string>
                {
                    {Language.English, "Wait time between retries."},
                    {Language.Chinese, "每次重试之间的等待时间。"}
                }
            },
            {"RequestTimeoutSec", new Dictionary<Language, string>
                {
                    {Language.English, "Request Timeout (s)"},
                    {Language.Chinese, "请求超时(秒)"}
                }
            },
            {"RequestTimeoutDescription", new Dictionary<Language, string>
                {
                    {Language.English, "Maximum wait time for a single request. Requests exceeding this time will be considered timed out."},
                    {Language.Chinese, "单个请求的最大等待时间。超过此时间将视为超时。"}
                }
            },
            {"APIConfigHelpText", new Dictionary<Language, string>
                {
                    {Language.English, "This configuration will be used for all AI API features, including automatic translation in the card editor.\nPlease ensure the API Key is set correctly and has sufficient quota."},
                    {Language.Chinese, "此配置将用于所有使用AI API的功能，包括卡牌编辑器的自动翻译等。\n请确保API Key已正确设置并且有足够的配额。"}
                }
            },
            {"TestingConnection", new Dictionary<Language, string>
                {
                    {Language.English, "Testing connection..."},
                    {Language.Chinese, "正在测试连接..."}
                }
            },
            {"ConnectionTestSuccess", new Dictionary<Language, string>
                {
                    {Language.English, "API connection test successful!"},
                    {Language.Chinese, "API连接测试成功！"}
                }
            },
            {"ConnectionTestFailed", new Dictionary<Language, string>
                {
                    {Language.English, "API connection test failed: {0}"},
                    {Language.Chinese, "API连接测试失败：{0}"}
                }
            },
            
            // 飞书同步窗口
            {"LocalizationTablesSection", new Dictionary<Language, string>
                {
                    {Language.English, "Localization Tables"},
                    {Language.Chinese, "本地化表"}
                }
            },
            {"TableName", new Dictionary<Language, string>
                {
                    {Language.English, "Table Name"},
                    {Language.Chinese, "表格名称"}
                }
            },
            {"NoLocalizationTablesWarning", new Dictionary<Language, string>
                {
                    {Language.English, "No localization tables found. Please create a localization table first."},
                    {Language.Chinese, "未找到本地化表，请先创建本地化表"}
                }
            },
            {"SyncOperations", new Dictionary<Language, string>
                {
                    {Language.English, "Sync Operations"},
                    {Language.Chinese, "同步操作"}
                }
            },
            {"LoadingTableList", new Dictionary<Language, string>
                {
                    {Language.English, "Loading table list..."},
                    {Language.Chinese, "正在加载表格列表..."}
                }
            },
            {"LoadingLocalizations", new Dictionary<Language, string>
                {
                    {Language.English, "Loading localization data..."},
                    {Language.Chinese, "正在加载本地化数据..."}
                }
            },
            {"PushingToFeishu", new Dictionary<Language, string>
                {
                    {Language.English, "Pushing to Feishu..."},
                    {Language.Chinese, "正在推送到飞书..."}
                }
            },
            {"PushSuccess", new Dictionary<Language, string>
                {
                    {Language.English, "Data successfully pushed to Feishu"},
                    {Language.Chinese, "数据已成功推送到飞书"}
                }
            },
            {"PushFailed", new Dictionary<Language, string>
                {
                    {Language.English, "Push failed: {0}"},
                    {Language.Chinese, "推送失败: {0}"}
                }
            },
            {"PullingFromFeishu", new Dictionary<Language, string>
                {
                    {Language.English, "Pulling from Feishu..."},
                    {Language.Chinese, "正在从飞书拉取数据..."}
                }
            },
            {"PullSuccess", new Dictionary<Language, string>
                {
                    {Language.English, "Data successfully pulled from Feishu"},
                    {Language.Chinese, "数据已成功从飞书拉取"}
                }
            },
            {"PullFailed", new Dictionary<Language, string>
                {
                    {Language.English, "Pull failed: {0}"},
                    {Language.Chinese, "拉取失败: {0}"}
                }
            },
            {"SyncingData", new Dictionary<Language, string>
                {
                    {Language.English, "Syncing data..."},
                    {Language.Chinese, "正在同步数据..."}
                }
            },
            {"SyncSuccess", new Dictionary<Language, string>
                {
                    {Language.English, "Data synchronization complete"},
                    {Language.Chinese, "数据同步完成"}
                }
            },
            {"SyncFailed", new Dictionary<Language, string>
                {
                    {Language.English, "Sync failed: {0}"},
                    {Language.Chinese, "同步失败: {0}"}
                }
            },
            {"ReloadingLocalizations", new Dictionary<Language, string>
                {
                    {Language.English, "Reloading localization data..."},
                    {Language.Chinese, "正在重新加载本地化数据..."}
                }
            },
            {"ReloadSuccess", new Dictionary<Language, string>
                {
                    {Language.English, "Localization data reloaded"},
                    {Language.Chinese, "本地化数据已重新加载"}
                }
            },
            {"ReloadFailed", new Dictionary<Language, string>
                {
                    {Language.English, "Reload failed: {0}"},
                    {Language.Chinese, "重新加载失败: {0}"}
                }
            },
            
            // 通用按钮和提示
            {"Completed", new Dictionary<Language, string>
                {
                    {Language.English, "Completed"},
                    {Language.Chinese, "已完成"}
                }
            },
            {"Cancelled", new Dictionary<Language, string>
                {
                    {Language.English, "Cancelled"},
                    {Language.Chinese, "已取消"}
                }
            },
            {"TranslationResult", new Dictionary<Language, string>
                {
                    {Language.English, "Translation {0}!\nSuccessfully translated: {1} items\nUnfinished: {2} items\nTotal: {3} items"},
                    {Language.Chinese, "已{0}翻译！\n成功翻译: {1} 个条目\n未完成: {2} 个条目\n总计: {3} 个条目"}
                }
            },
        };

        /// <summary>
        /// 初始化语言设置
        /// </summary>
        static I18N()
        {
            // 从EditorPrefs加载语言设置
            if (EditorPrefs.HasKey("LocalizationTranslator_Language"))
            {
                _currentLanguage = (Language)EditorPrefs.GetInt("LocalizationTranslator_Language");
            }
        }

        /// <summary>
        /// 获取指定键的本地化文本
        /// </summary>
        /// <param name="key">文本键</param>
        /// <returns>本地化文本</returns>
        public static string T(string key)
        {
            if (_translations.TryGetValue(key, out var languageDict) && 
                languageDict.TryGetValue(_currentLanguage, out var text))
            {
                return text;
            }
            
            // 如果找不到翻译，返回键名
            return key;
        }
    }
} 