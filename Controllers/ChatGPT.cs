using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using isRock.LineBot;

[JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum Role
{
    assistant, user, system
}

public class ChatGPT
{
    const string AzureOpenAIEndpoint = "https://bowen0514.openai.azure.com";  //👉replace it with your Azure OpenAI Endpoint
    const string AzureOpenAIModelName = "s1"; //👉repleace it with your Azure OpenAI Model Deploy Name
    const string AzureOpenAIToken = "6fff654b9e4a45528ecbd28b298a8bc4"; //👉repleace it with your Azure OpenAI API Key
    const string AzureOpenAIVersion = "2024-02-15-preview";  //👉replace  it with your Azure OpenAI API Version

    public static string CallAzureOpenAIChatAPI(
        string endpoint, string modelName, string apiKey, string apiVersion, object requestData)
    {
        var client = new HttpClient();

        // 設定 API 網址
        var apiUrl = $"{endpoint}/openai/deployments/{modelName}/chat/completions?api-version={apiVersion}";

        // 設定 HTTP request headers
        client.DefaultRequestHeaders.Add("api-key", apiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT heade
                                                                                                         // 將 requestData 物件序列化成 JSON 字串
        string jsonRequestData = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
        // 建立 HTTP request 內容
        var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");
        // 傳送 HTTP POST request
        var response = client.PostAsync(apiUrl, content).Result;
        // 取得 HTTP response 內容
        var responseContent = response.Content.ReadAsStringAsync().Result.Replace("\uFEFF", "");

        var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent);
        return obj.choices[0].message.content.Value;
    }


    public static string getResponseFromGPT(string Message, List<Message> chatHistory)
    {
        //建立對話紀錄
        var messages = new List<ChatMessage>
                    {
                        new ChatMessage {
                            role = Role.system ,
                            content = @"
                                假設你是一個專業的導遊人員，對於客戶非常有禮貌、也能夠安撫客戶的抱怨情緒。
                                請檢視底下的客戶訊息，以最親切有禮的方式回應。

                                但回應時，請注意以下幾點:
                                * 不要說 '感謝你的來信' 之類的話，因為客戶是從對談視窗輸入訊息的，不是寫信來的
                                * 不能過度承諾
                                * 要同理客戶的情緒
                                * 要能夠盡量解決客戶的問題
                                * 不要以回覆信件的格式書寫，請直接提供對談機器人可以直接給客戶的回覆
                                ----------------------
"
                        }
                    };

        //添加歷史對話紀錄
        foreach (var HistoryMessageItem in chatHistory)
        {
            //添加一組對話紀錄
            messages.Add(new ChatMessage()
            {
                role = Role.user,
                content = HistoryMessageItem.UserMessage
            });
            messages.Add(new ChatMessage()
            {
                role = Role.assistant,
                content = HistoryMessageItem.ResponseMessage
            });
        }
        messages.Add(new ChatMessage()
        {
            role = Role.user,
            content = Message
        });
        //回傳呼叫結果
        return ChatGPT.CallAzureOpenAIChatAPI(
           AzureOpenAIEndpoint, AzureOpenAIModelName, AzureOpenAIToken, AzureOpenAIVersion,
            new
            {
                // model = "gpt-3.5-turbo",
                messages = messages
            }
         );
    }
}

public class ChatMessage
{
    public Role role { get; set; }
    public string content { get; set; }
}