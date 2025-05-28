using UnityEngine;
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEditor;

namespace CardGame.Editor.LLMAI
{
    public class LLMAIService
    {
        private static LLMAIService instance;
        private static Dictionary<string, string> scriptPathCache = new Dictionary<string, string>();
        private static bool hasInitializedCache = false;

        // 初始化脚本路径缓存
        private void InitializeScriptCache()
        {
            if (hasInitializedCache) return;
            
            scriptPathCache.Clear();
            var guids = AssetDatabase.FindAssets("t:MonoScript");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                scriptPathCache[fileName] = path;
            }
            hasInitializedCache = true;
        }

        public static LLMAIService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LLMAIService();
                    instance.InitializeScriptCache();
                }
                return instance;
            }
        }

        [Serializable]
        private class Message
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class Choice
        {
            public Message message;
        }

        [Serializable]
        private class LLMAIRequest
        {
            public string model;
            public List<Message> messages;
            public float temperature = 0.7f;
            public int max_tokens = 1000;
        }

        [Serializable]
        private class LLMAIResponse
        {
            public List<Choice> choices;
        }

        /// <summary>
        /// 使用AI服务翻译文本
        /// </summary>
        /// <param name="text">要翻译的文本</param>
        /// <param name="targetLanguage">目标语言</param>
        /// <param name="localizationKey">本地化Key（可选）</param>
        /// <param name="description">描述信息（可选）</param>
        /// <param name="systemPromptOverride">系统提示词覆盖（可选）</param>
        /// <param name="userPromptOverride">用户提示词覆盖（可选）</param>
        /// <param name="additionalContext">额外的上下文信息（可选）</param>
        /// <returns>翻译后的文本</returns>
        public async Task<string> TranslateText(
            string text, 
            string targetLanguage, 
            string localizationKey = null, 
            string description = null,
            string systemPromptOverride = null,
            string userPromptOverride = null,
            string additionalContext = null)
        {
            // 如果文本为空，直接返回空字符串而不进行翻译
            if (string.IsNullOrEmpty(text))
            {
                Debug.Log($"[AI翻译] 跳过空文本翻译，本地化Key: {localizationKey}");
                return string.Empty;
            }

            string userPrompt = userPromptOverride ?? text;
            string systemPrompt;

            if (systemPromptOverride != null)
            {
                systemPrompt = systemPromptOverride;
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"You are a professional game translator. Your task is to translate the text provided in the USER'S MESSAGE to {targetLanguage}.");

                bool hasDescription = !string.IsNullOrEmpty(description);
                bool hasLocalizationInfo = !string.IsNullOrEmpty(localizationKey) && !string.IsNullOrEmpty(additionalContext);

                if (hasDescription || hasLocalizationInfo)
                {
                    sb.AppendLine("This is contextual information to aid your translation of the USER'S MESSAGE. DO NOT translate this contextual information; it is for your understanding only:");
                    if (hasDescription)
                    {
                        sb.AppendLine($"- Context Description: {description}");
                    }
                    if (hasLocalizationInfo)
                    {
                        sb.AppendLine($"- Localization Info: {additionalContext}");
                    }
                }
                
                sb.Append($"Translate ONLY the USER'S MESSAGE. Maintain original style and formatting. Respond with only the translated text, no explanations or apologies.");
                systemPrompt = sb.ToString();
            }

            var messages = new List<Message>
            {
                new Message
                {
                    role = "system",
                    content = systemPrompt
                },
                new Message
                {
                    role = "user",
                    content = userPrompt
                }
            };

            // 添加日志输出
            Debug.Log($"[AI翻译] 发送翻译请求:\n系统提示词: {systemPrompt}\n用户提示词: {userPrompt}");

            var requestData = new LLMAIRequest
            {
                model = LLMAIConfig.Instance.modelName,
                messages = messages
            };

            string jsonRequest = JsonUtility.ToJson(requestData);
            Debug.Log($"[AI翻译] 翻译配置:\n" +
                     $"目标语言: {targetLanguage}\n" +
                     $"本地化Key: {localizationKey}\n" +
                     $"系统提示词: {systemPrompt}\n" +
                     $"用户提示词: {userPrompt}");

            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);

            using (var request = new UnityWebRequest(LLMAIConfig.Instance.apiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {LLMAIConfig.Instance.apiKey}");
                try
                {
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        throw new Exception($"API调用失败: {request.error}\n{request.downloadHandler.text}");
                    }

                    var response = JsonUtility.FromJson<LLMAIResponse>(request.downloadHandler.text);
                    if (response.choices != null && response.choices.Count > 0)
                    {
                        return response.choices[0].message.content.Trim();
                    }
                    else
                    {
                        throw new Exception("API返回的响应格式不正确");
                    }
                }
                finally
                {
                    request.Dispose();
                }
            }
        }
    }
} 