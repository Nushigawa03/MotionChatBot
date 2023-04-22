using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace MotionChat
{
    public class APIClient : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            Chat();
            Get();
        }

        // Update is called once per frame
        void Update()
        {
            
        }
        
        public static async Task Get()
        {
            // HTTPクライアントの作成
            using var client = new HttpClient();

            // FastAPIのエンドポイントにリクエストを送信するためのHTTPリクエストを構築
            var uriBuilder = new UriBuilder("http://localhost:8000/MDM/");
            var parameters = System.Web.HttpUtility.ParseQueryString(string.Empty);
            parameters["text_prompt"] = "The person suddenly dances while walking.";
            uriBuilder.Query = parameters.ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.ToString());

            // HTTPクライアントを使用してHTTPリクエストを送信し、FastAPIからのHTTPレスポンスを受け取る
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();

            Debug.Log(responseContent);
        }

        public static async Task Chat()
        {
            // APIキーを設定
            string apiKey = "";

            // APIエンドポイントを設定
            string endpoint = "https://api.openai.com/v1/engines/davinci-codex/completions";

            // HTTPクライアントを作成
            using var httpClient = new HttpClient();

            // リクエストヘッダーを設定
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            // リクエストボディを作成
            string requestBody = "{\"prompt\": \"Hello, world!\", \"max_tokens\": 5}";

            // APIを呼び出し
            var response = await httpClient.PostAsync(endpoint, new StringContent(requestBody));

            // レスポンスをJSONとしてパース
            var responseContent = await response.Content.ReadAsStringAsync();

            Debug.Log(responseContent);
        }
    }
}