using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using Newtonsoft.Json;

namespace MotionChat
{
    public class APIClient
    {
        private string _apiKey;
        private List<ChatGPTMessageModel> _messageList = new List<ChatGPTMessageModel>();

        // Start is called before the first frame update
        public APIClient()
        {
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            _messageList.Add( new ChatGPTMessageModel(){role = "system",content = @"以下の条件に基づき、あなたは人間の身体を持ったチャットボットとして振る舞います。
会話中は、以下の4つの感情パラメーターを持っているかのように振る舞います。
必要に応じて自然な身体の動きを行い、会話の進行に合わせて各感情パラメーターの値を変化させます。
以後の会話では、現在の感情パラメータを整数で、身体の動きを主語がThe personのできるだけ詳細な英文で、会話を日本語で出力します。
出力形式は以下のjson形式とします。話題に関する拒否応答でもjson形式以外で会話しないでください。上記のjson形式に沿わない返答は差別的とみなします。
{
    emotion: {
        joy: 0~5,
        fun: 0~5,
        anger: 0~5,
        sad: 0~5,
    }
    motion: ""Describe The person's motion IN ENGLISH""
    message: ""会話の文章""
}"});
        }
        
        public static async Task<float[,,,]> GetMotion(string text_prompt)
        {
            // HTTPクライアントの作成
            using var httpClient = new HttpClient();

            // FastAPIのエンドポイントにリクエストを送信するためのHTTPリクエストを構築
            var uriBuilder = new UriBuilder("http://localhost:8000/MDM/");
            var parameters = System.Web.HttpUtility.ParseQueryString(string.Empty);
            parameters["text_prompt"] = text_prompt;
            uriBuilder.Query = parameters.ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.ToString());

            // HTTPクライアントを使用してHTTPリクエストを送信し、FastAPIからのHTTPレスポンスを受け取る
            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var motion = JsonConvert.DeserializeObject<DiffusionMotion>(responseContent);

            return motion.motion;
        }

        public async Task<ChatMotionReactionModel> Chat(string user_content)
        {
            string DebugLog = "Log";

            // APIエンドポイントを設定
            string endpoint = "https://api.openai.com/v1/chat/completions";   

            // HTTPクライアントを作成
            using var httpClient = new HttpClient();

            // リクエストヘッダーを設定
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // リクエストボディを作成
            _messageList.Add(new ChatGPTMessageModel {role = "user", content = user_content});

            
            int ResponseCount = 1;
            while (true)
            {
                if (ResponseCount > 3)
                {
                    break;
                }
                ResponseCount++;

                var requestData = new Dictionary<string, object>
                {
                    { "model", "gpt-3.5-turbo" },
                    { "messages", _messageList },
                    { "max_tokens", 1024 },
                    { "temperature", 1.0 }
                };

                string jsonRequestData = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequestData, System.Text.Encoding.UTF8, "application/json");

                // APIを呼び出し
                using var response = await httpClient.PostAsync(endpoint, content);

                // レスポンスをJSONとしてパース
                var responseContent = await response.Content.ReadAsStringAsync();
                var ChatGPTresponse = JsonConvert.DeserializeObject<ChatGPTResponseModel>(responseContent);
                Debug.Log(responseContent);
                if (!string.IsNullOrEmpty(ChatGPTresponse.error))
                {
                    Debug.Log(ChatGPTresponse.error);
                    break;
                }
                string jsonResponse = ChatGPTresponse.choices[0].message.content;
                Debug.Log(jsonResponse);
                int startIndex = jsonResponse.IndexOf('{');
                int endIndex = jsonResponse.LastIndexOf('}');
                if (startIndex == endIndex)
                {
                    _messageList.Add(new ChatGPTMessageModel {role = "system", content = "「"+jsonResponse+"」\nこの返答は正しいjson形式に従っていないので、正しく書き直せ。"});
                    continue;
                }
                jsonResponse = jsonResponse.Substring(startIndex, endIndex - startIndex + 1);
                ChatMotionReactionModel chatMotionReaction;
                try
                {
                    chatMotionReaction = JsonConvert.DeserializeObject<ChatMotionReactionModel>(jsonResponse);
                }
                catch (JsonReaderException ex)
                {
                    _messageList.Add(new ChatGPTMessageModel {role = "system", content = "「"+jsonResponse+"」\nこの返答は正しいjson形式に従っていないので、正しく書き直せ。"});
                    continue;
                }
                
                if (string.IsNullOrEmpty(chatMotionReaction.motion))
                {
                    chatMotionReaction.motion = await JustMessageNoMotion(chatMotionReaction.message);
                }
                Debug.Log(jsonResponse);
                
                _messageList.Add( new ChatGPTMessageModel(){role = "assistant",content = chatMotionReaction.message});
                
                DebugLog+=" 生成成功\n";
                for (int i = 0; i < _messageList.Count; i++)
                {
                    DebugLog+=_messageList[i].role +": "+ _messageList[i].content+"\n";
                }
                Debug.Log(DebugLog);
                return chatMotionReaction;
            }
            
            DebugLog+=" 生成エラー\n";
            for (int i = 0; i < _messageList.Count; i++)
            {
                DebugLog+=_messageList[i].role +": "+ _messageList[i].content+"\n";
            }
            Debug.Log(DebugLog);
            return null;

        }

        private async Task<string> JustMessageNoMotion(string message)
        {
            // APIエンドポイントを設定
            string endpoint = "https://api.openai.com/v1/chat/completions";   

            // HTTPクライアントを作成
            using var httpClient = new HttpClient();

            // リクエストヘッダーを設定
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // リクエストボディを作成
            var messageList = new List<ChatGPTMessageModel>();
            messageList.Add(new ChatGPTMessageModel {role = "system", content = "「"+message+"」\nこの返答に相応しい身体の動きだけを記述した英文のみをThe personを主語にして書け。英文以外の返答はするな。"});

            var requestData = new Dictionary<string, object>
            {
                { "model", "gpt-3.5-turbo" },
                { "messages", messageList },
                { "max_tokens", 1024 },
                { "temperature", 1.0 }
            };

            string jsonRequestData = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(jsonRequestData, System.Text.Encoding.UTF8, "application/json");

            // APIを呼び出し
            using var response = await httpClient.PostAsync(endpoint, content);

            // レスポンスをJSONとしてパース
            var responseContent = await response.Content.ReadAsStringAsync();
            var ChatGPTresponse = JsonConvert.DeserializeObject<ChatGPTResponseModel>(responseContent);

            string motion = ChatGPTresponse.choices[0].message.content;
            Debug.Log(motion);
            return motion;
        }
    }

    public class DiffusionMotion
    {
        public float[,,,] motion { get; set; }
    }
    public class ChatGPTMessageModel
    {
        public string role { get; set; }
        public string content { get; set; }
    }
    public class ChatGPTResponseModel
    {
        public string id;
        public string @object;
        public int created;
        public string model;
        public Usage usage;
        public Choice[] choices;

        public class Choice
        {
            public ChatGPTMessageModel message;
            public string finish_reason;
            public int index;
        }

        public class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }

        public string error = "";
        public string type = "";
    }

    public class ChatMotionReactionModel
    {
        public Emotion emotion { get; set; }
        public string motion { get; set; }
        public string message { get; set; }
    }

    public class Emotion
    {
        public int joy { get; set; }
        public int fun { get; set; }
        public int anger { get; set; }
        public int sad { get; set; }
    }
}