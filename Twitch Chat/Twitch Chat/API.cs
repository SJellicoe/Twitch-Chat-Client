using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Chat
{
    class API
    {
        public bool Connect()
        {
            bool success = false;

            using (HttpClient webClient = new HttpClient())
            {
                string uri = "https://api.twitch.tv/kraken";
                ASCIIEncoding encoder = new ASCIIEncoding();
                byte[] buffer;


                webClient.BaseAddress = new Uri(uri);
                webClient.DefaultRequestHeaders.Accept.Clear();
                webClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = webClient.GetAsync("?oauth_token=" + ConfigurationManager.AppSettings["oauth"]).Result;

                if (response.IsSuccessStatusCode)
                {
                    success = true;
                    buffer = response.Content.ReadAsByteArrayAsync().Result;
                }
                else
                {
                    success = false;
                }
            }

            return success;
        }
    }
}
