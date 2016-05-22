using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Twitch_Chat.Twtich_API.Models;
using Twitch_Chat.Twtich_API.ViewModels;

namespace Twitch_Chat
{
    public class API
    {
        private const string BASE_URL = "https://api.twitch.tv/kraken/";
        private string _oauthToken = "";

        public async Task<bool> Connect()
        {
            bool success = false;

            using (HttpClient webClient = CreateHttpClient())
            {
                HttpResponseMessage response = await webClient.GetAsync($"?oauth_token={_oauthToken}");

                if (response.IsSuccessStatusCode)
                {
                    string buffer = await response.Content.ReadAsStringAsync();
                    success = true;
                }
            }

            return success;
        }

        public async Task<bool> UpdateTitle(string title)
        {
            bool success = false;
            channel channel = new channel() { status = title };

            using (HttpClient webClient = CreateHttpClient())
            {
                string endpoint = $"channels/{ConfigurationManager.AppSettings["username"]}";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, endpoint);
                request.Content = new StringContent(JsonConvert.SerializeObject(new ChannelViewModel() { channel = channel }), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await webClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    success = true;
                }
            }

            return success;
        }

        public async Task<bool> CallMethod(string endpoint, string value, string oauthToken)
        {
            _oauthToken = oauthToken;
            bool success = false;

            if (endpoint.StartsWith("/title"))
            {
                success = await UpdateTitle(value);
            }

            return success;
        }

        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient();

            client.BaseAddress = new Uri(BASE_URL);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.twitchtv.v3+json"));
            client.DefaultRequestHeaders.Add("Client-ID", ConfigurationManager.AppSettings["clientId"]);
            client.DefaultRequestHeaders.Add("Authorization", $"OAuth {_oauthToken}");

            return client;
        }
    }
}
