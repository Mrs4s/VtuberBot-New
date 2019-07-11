using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VtuberBot.Core.Extensions;

namespace VtuberBot.Spider.Services.Twitter
{
    public class TweetCard
    {
        public long Id { get; set; }

        public string Content { get; set; }

        public string ScreenName { get; set; }

        public string DisplayName { get; set; }

        [JsonIgnore]
        public string MinPosition { get; set; }

        [JsonIgnore]
        public List<TweetCard> RootComments { get; set; }

        public async Task<List<TweetCard>> GetAllCommentAsync()
        {
            using (var client = HttpClientExtensions.CreateClient(referer: "https://twitter.com/" + ScreenName, useGZip: true))
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");
                client.DefaultRequestHeaders.Add("x-overlay-request", "true");
                client.DefaultRequestHeaders.Add("x-previous-page-name", "profile");
                client.DefaultRequestHeaders.Add("x-twitter-active-user", "yes");
                var json = JToken.Parse(await client.GetStringAsync(
                    $"https://twitter.com/i/{ScreenName}/conversation/{Id}?include_available_features=1&include_entities=1&max_position={MinPosition}&reset_error_state=false"));
                var comments = new List<TweetCard>();
                string minPosition;
                do
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(json["items_html"].ToString());
                    minPosition = json["min_position"]?.ToString();
                    foreach (var node in doc.DocumentNode.ChildNodes.Where(v => v.Name == "li"))
                    {
                        var context = node.ChildNodes.First(v => v.Name == "ol").ChildNodes
                            .FirstOrDefault(v => v.Name == "li");
                        if (context == null || context.Attributes.First(v => v.Name == "class").Value == "ThreadedConversation-moreReplies")
                        {
                            context = node.ChildNodes
                                .First(v => v.Name == "ol").ChildNodes
                                .First(v => v.Name == "div").ChildNodes.First(v => v.Name == "li");
                        }

                        var id = context.Attributes.FirstOrDefault(v => v.Name == "data-item-id")?.Value;
                        var info = context.ChildNodes.First(v => v.Name == "div");
                        var authorDisplayName = info.Attributes.First(v => v.Name == "data-name").Value;
                        var message = info.ChildNodes.First(v => v.Name == "div" && v.ChildNodes.Count > 2)
                            .ChildNodes
                            .First(v => v.Attributes.Any(att =>
                                att.Name == "class" && att.Value == "js-tweet-text-container")).ChildNodes
                            .First(v => v.Name == "p");
                        var authorScreenName = info.ChildNodes.First(v => v.Name == "div" && v.ChildNodes.Count > 2)
                            .ChildNodes
                            .First(v => v.Attributes.Any(att =>
                                att.Name == "class" && att.Value == "stream-item-header")).ChildNodes
                            .First(v => v.Name == "a").Attributes.First(v => v.Name == "href").Value;
                        comments.Add(new TweetCard()
                        {
                            Id = long.Parse(id),
                            DisplayName = authorDisplayName,
                            ScreenName = authorScreenName.Substring(1, authorScreenName.Length - 1),
                            Content = message.InnerText
                        });
                    }

                    json = JToken.Parse(await client.GetStringAsync(
                        $"https://twitter.com/i/{ScreenName}/conversation/{Id}?include_available_features=1&include_entities=1&max_position={minPosition}&reset_error_state=false"));
                } while (!string.IsNullOrEmpty(minPosition));

                return comments.Union(RootComments).ToList();
            }
        }
    }
}
