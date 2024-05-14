using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace isRock.Template
{
    public class LineBotChatGPTWebHookController : isRock.LineBot.LineWebHookControllerBase
    {
        [Route("api/LineBotChatGPTWebHook")]
        [HttpPost]
        public IActionResult POST()
        {
            const string AdminUserId = "U1e72f08d82273fefe45c57ed9228088e"; //👉repleace it with your Admin User Id

            try
            {
                //設定ChannelAccessToken
                this.ChannelAccessToken = ""; //👉repleace it with your Channel Access Token
                //配合Line Verify
                if (ReceivedMessage.events == null || ReceivedMessage.events.Count() <= 0 ||
                    ReceivedMessage.events.FirstOrDefault().replyToken == "00000000000000000000000000000000") return Ok();
                //取得Line Event
                var LineEvent = this.ReceivedMessage.events.FirstOrDefault();
                var responseMsg = "";
                //準備回覆訊息
                if (LineEvent.type.ToLower() == "message" && LineEvent.message.type == "text")
                {
                    //add history message
                    ChatGPT.conversationHistory.Add(new Dictionary<ChatGPT.role, string> { { ChatGPT.role.system, LineEvent.message.text } });
                    responseMsg = ChatGPT.getResponseFromGPT(LineEvent.message.text);
                }
                else if (LineEvent.type.ToLower() == "message")
                    responseMsg = $"收到 event : {LineEvent.type} type: {LineEvent.message.type} ";
                else
                    responseMsg = $"收到 event : {LineEvent.type} ";
                //回覆訊息
                this.ReplyMessage(LineEvent.replyToken, responseMsg);
                //response OK
                return Ok();
            }
            catch (Exception ex)
            {
                //回覆訊息
                this.PushMessage(AdminUserId, "發生錯誤:\n" + ex.Message);
                //response OK
                return Ok();
            }
        }
    }

    public class ChatGPT
    {
        const string AzureOpenAIEndpoint = "https://testaoai20240508.openai.azure.com";  //👉replace it with your Azure OpenAI Endpoint
        const string AzureOpenAIModelName = "gpt3516k"; //👉repleace it with your Azure OpenAI Model Deploy Name
        const string AzureOpenAIToken = ""; //👉repleace it with your Azure OpenAI API Key
        const string AzureOpenAIVersion = "2024-02-15-preview";  //👉replace  it with your Azure OpenAI API Version

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public enum role
        {
            assistant, user, system
        }

        public static string CallAzureOpenAIChatAPI(
            string endpoint, string DeploymentName, string apiKey, string apiVersion, object requestData)
        {
            var client = new HttpClient();

            // 設定 API 網址
            var apiUrl = $"{endpoint}/openai/deployments/{DeploymentName}/chat/completions?api-version={apiVersion}";

            // 設定 HTTP request headers
            client.DefaultRequestHeaders.Add("api-key", apiKey); //👉Azure OpenAI key
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
            // 將 requestData 物件序列化成 JSON 字串
            string jsonRequestData = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
            // 建立 HTTP request 內容
            var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");
            // 傳送 HTTP POST request
            var response = client.PostAsync(apiUrl, content).Result;
            // 取得 HTTP response 內容
            var responseContent = response.Content.ReadAsStringAsync().Result;
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent);
            //add history message
            conversationHistory.Add(new Dictionary<role, string> { { role.assistant, obj.choices[0].message.content.Value } });


            return obj.choices[0].message.content.Value;
        }

        public static string CallOpenAIChatAPI(string apiKey, object requestData)
        {
            var client = new HttpClient();

            // 設定 API 網址
            var apiUrl = $"https://api.openai.com/v1/chat/completions";

            // 設定 HTTP request headers
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}"); //👉OpenAI key
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
            // 將 requestData 物件序列化成 JSON 字串
            string jsonRequestData = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
            // 建立 HTTP request 內容
            var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");
            // 傳送 HTTP POST request
            var response = client.PostAsync(apiUrl, content).Result;
            // 取得 HTTP response 內容
            var responseContent = response.Content.ReadAsStringAsync().Result;
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent);
            return obj.choices[0].message.content.Value;
        }

        //create static string array to store the conversation history with role and content
        public static List<Dictionary<role, string>> conversationHistory = new List<Dictionary<role, string>>();

        public static string getResponseFromGPT(string Message)
        {
            //create messages array to call open ai
            var messages = new[]
                    {
                        //system message
                        new {
                            role = ChatGPT.role.system ,
                            content = @"
                            你是餐飲店的客服機器人，會回答客戶詢問的餐點問題，並且推薦讓店裡獲利最高的產品。你也能處裡客戶的抱怨和申訴，安撫客戶情緒。
                            
                            目前店裡面所販賣的產品有底下這些:
                            大亨堡，45元
                            麥香雞，36元
                            蛋餅，27元
                            可麗餅，50元(素食)
                            飯糰，45元(素食)
                            
                            而飲料有底下這些:
                            可樂，55元(素食)
                            紅茶，35元(素食者可用)
                            奶茶，45元(素食)
                            店裡沒有套餐，只能把餐點和飲料搭配銷售，這樣搭配沒有折扣。此外，跟客人推薦餐點時，必須是上述的清單中的餐點。
                            目前唯一的優惠是周日早上買一送一，只限單點大亨堡才有這個優惠。
                            ----------------------
"
                        },
                        //history message
                        new {
                            role = ChatGPT.role.user,
                            content = "請告訴我最便宜的餐點價格。"
                        },
                        new {
                            role = ChatGPT.role.assistant,
                            content = "你可以點選蛋餅，只需要27元"
                        },
                        new {
                            role = ChatGPT.role.user,
                            content = "有什麼好吃的餐點嗎?"
                        },
                        new {
                            role = ChatGPT.role.assistant,
                            content = @"這個問題很主觀，不同的人有不同的口味喜好，以下是我個人的推薦:
                                1. 大亨堡：這是我們餐廳的招牌餐點，肉質鮮美，搭配特製的醬料非常美味
                                2. 麥香雞：這是一個很受歡迎的餐點，雞肉鮮嫩，外皮酥脆，非常適合當作午餐或晚餐
                                3. 可麗餅：這是一個很受歡迎的早餐餐點，口感軟嫩，搭配楓糖漿和奶油非常美味"
                        },
                        new {
                            role = ChatGPT.role.user,
                            content = "我想要吃飽一點，你可以推薦我豐盛的套餐嗎?"
                        },
                        new {
                            role = ChatGPT.role.assistant,
                            content = @"很抱歉，我們店裡沒有套餐，只能把餐點和飲料搭配銷售。
                                但是，如果你想吃得飽一點，我建議你可以考慮以下搭配：
                                1. 大亨堡 + 可樂：肉質鮮美，搭配可樂口感更佳。
                                2. 麥香雞 + 紅茶：雞肉鮮嫩，搭配紅茶口感更清爽。
                                3. 飯糰 + 奶茶：飯糰營養豐富，搭配奶茶口感更香濃。
                                希望這些搭配可以滿足你的食慾需求！"
                        },
                        new {
                            role = ChatGPT.role.user,
                            content = "這些搭配套餐分別是多少錢?"
                        },
                        new {
                            role = ChatGPT.role.assistant,
                            content = @"不好意思，我剛才的回答有點不清楚，我重新提供給你更詳細的價格資訊:
                                1. 大亨堡 + 可樂 = 100元 (大亨堡 45元 + 可樂 55元)
                                2. 麥香雞 + 紅茶 = 71元 (麥香雞 36元 + 紅茶 35元)
                                3. 飯糰 + 奶茶 = 90元 (飯糰 45元 + 奶茶 45元)
                                希望這些資訊可以幫助到你！"
                        },
                    };
            //get messages from conversationHistory
            foreach (var item in conversationHistory)
            {
                //get key from dictionary
                var key = item.Keys.First();
                //get value from dictionary
                var value = item.Values.First();
                //add history message
                messages = messages.Concat(new[] { new { role = key, content = value } }).ToArray();
            }
            //add user message to messages
            messages = messages.Concat(new[] { new { role = ChatGPT.role.user, content = Message } }).ToArray();


            return ChatGPT.CallAzureOpenAIChatAPI(
               AzureOpenAIEndpoint, AzureOpenAIModelName, AzureOpenAIToken, AzureOpenAIVersion,
                //ref: https://learn.microsoft.com/en-us/azure/cognitive-services/openai/reference#chat-completions
                new
                {
                    //model = "gpt-3.5-turbo",
                    messages = messages

                });
        }
    }
}