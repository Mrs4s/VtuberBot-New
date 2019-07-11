using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
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

        public static async Task<TweetCard> GetTweetCardAsync(string screenName, long id)
        {
            using (var client = HttpClientExtensions.CreateClient(referer: "https://twitter.com/" + screenName, useGZip: true))
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");
                client.DefaultRequestHeaders.Add("x-overlay-request", "true");
                client.DefaultRequestHeaders.Add("x-previous-page-name", "profile");
                client.DefaultRequestHeaders.Add("x-twitter-active-user", "yes");
                var json = JToken.Parse(await client.GetStringAsync(
                    $"https://twitter.com/{screenName}/status/{id}?conversation_id={id}"));
                var doc = new HtmlDocument();
                doc.LoadHtml(json["page"].ToString());
                var content = doc.DocumentNode.SelectSingleNode("/div/div[1]/div[1]/div/div[2]/p")?.InnerText;
                var minPosition = doc.DocumentNode.SelectSingleNode("//*[@id=\"descendants\"]/div").Attributes.First(v => v.Name == "data-min-position").Value;
                var rootComments = new List<TweetCard>();
                var commentsNode = doc.DocumentNode.SelectSingleNode("//*[@id=\"stream-items-id\"]");
                foreach (var threadNode in commentsNode.ChildNodes.Where(v => v.Name == "li"))
                {
                    foreach (var divNode in threadNode.ChildNodes.First(v => v.Name == "ol").ChildNodes.Where(v => v.Name == "div"))
                    {
                        var a = divNode.ChildNodes.First(v => v.Name == "li").ChildNodes.First(v => v.Name == "div");
                        var commentTweetId = a.Attributes.First(v => v.Name == "data-tweet-id").Value;
                        var authorDisplayName = a.Attributes.First(v => v.Name == "data-name").Value;
                        var message = a.ChildNodes.First(v => v.Name == "div" && v.ChildNodes.Count > 2).ChildNodes
                            .First(v => v.Attributes.Any(att =>
                                att.Name == "class" && att.Value == "js-tweet-text-container")).ChildNodes
                            .First(v => v.Name == "p");
                        var authorScreenName = a.ChildNodes.First(v => v.Name == "div" && v.ChildNodes.Count > 2).ChildNodes
                            .First(v => v.Attributes.Any(att =>
                                att.Name == "class" && att.Value == "stream-item-header")).ChildNodes
                            .First(v => v.Name == "a").Attributes.First(v => v.Name == "href").Value;
                        rootComments.Add(new TweetCard()
                        {
                            Id = long.Parse(commentTweetId),
                            DisplayName = authorDisplayName,
                            ScreenName = authorScreenName.Substring(1, authorScreenName.Length - 1),
                            Content = message.InnerText
                        });
                    }
                }
                return new TweetCard()
                {
                    Id = id,
                    Content = content,
                    ScreenName = screenName,
                    MinPosition = minPosition,
                    RootComments = rootComments
                };
            }
        }

        public static async Task<List<TimelineTweet>> GetTimelineByWebAsync(string screenName)
        {
            using (var client = HttpClientExtensions.CreateClient(referer: "https://twitter.com/" + screenName, useGZip: true))
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");
                client.DefaultRequestHeaders.Add("x-previous-page-name", "profile");
                client.DefaultRequestHeaders.Add("x-twitter-active-user", "yes");
                var json = JToken.Parse(await client.GetStringAsync(
                    $"https://twitter.com/i/profiles/show/{screenName}/timeline/tweets?include_available_features=1&include_entities=1&reset_error_state=false"));
                var doc = new HtmlDocument();
                doc.LoadHtml(json["items_html"].ToString());
                return (from liNode in doc.DocumentNode.ChildNodes.Where(v => v.Name == "li")
                        let pinned = liNode.Attributes.First(v => v.Name == "class").Value.Trim().EndsWith("pinned")
                        let id = long.Parse(liNode.Attributes.First(v => v.Name == "data-item-id").Value)
                        let div = liNode.ChildNodes.First(v => v.Name == "div").ChildNodes.Last(v => v.Name == "div")
                        let content = div.ChildNodes.First(v =>
                                v.Name == "div" && v.Attributes.Any(att => att.Value == "js-tweet-text-container"))
                            .InnerText
                            .Trim()
                        select new TimelineTweet()
                        {
                            Id = id,
                            Pinned = pinned,
                            ScreenName = screenName,
                            Retweeted = liNode.ChildNodes.First(v => v.Name == "div").ChildNodes.First(v => v.Name == "div")
                                .InnerText.Contains("Retweeted"),
                            Content = content
                        }).ToList();
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
