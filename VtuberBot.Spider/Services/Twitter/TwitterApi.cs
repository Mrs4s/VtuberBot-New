using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VtuberBot.Core.Extensions;

namespace VtuberBot.Spider.Services.Twitter
{
    public class TwitterApi
    {
        public static string AccessToken { get; private set; }

        public static string BaseToken = Config.DefaultConfig.TwitterAccessToken;

        public static async Task<List<TweetInfo>> GetTimelineByUserAsync(string username, int count = 5)
        {
            try
            {
                if (string.IsNullOrEmpty(AccessToken))
                    await InitAccessToken();
                using (var client = HttpClientExtensions.CreateClient(useGZip: true))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                    return JsonConvert.DeserializeObject<List<TweetInfo>>(await client.GetStringAsync(
                        $"https://api.twitter.com/1.1/statuses/user_timeline.json?count={count}&screen_name={username}"));
                }

            }
            catch
            {
                return null;
            }


        }

        public static async Task<string> GetTweetCardHtmlAsync(long id)
        {
            var url = "https://twitter.com/kmnzlita/status/" + id;
            using (var client = HttpClientExtensions.CreateClient(useGZip: true))
            {
                var json = JToken.Parse(await client.GetStringAsync(
                    $"https://publish.twitter.com/oembed?url={url.UrlEncode()}&theme=light&link_color=%23981CEB&lang=zh-cn"));
                return json["html"].ToString();
            }
        }




        public static async Task InitAccessToken()
        {
            using (var client = HttpClientExtensions.CreateClient(useGZip: true))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", BaseToken);
                var json = JObject.Parse(await (await client.PostFormAsync("https://api.twitter.com/oauth2/token",
                    new {grant_type = "client_credentials"})).Content.ReadAsStringAsync());
                AccessToken = json["access_token"].ToObject<string>();
            }
        }
    }
}
