using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VtuberBot.Spider.Services.Youtube
{
    public class YoutubeLiveChatInfo
    {
        [JsonProperty("pollingIntervalMillis")]
        public int PollingInterval { get; set; }

        [JsonProperty("items")]
        public JArray CommentsToken { get; set; }

        public YoutubeLiveChat[] GetComments()
        {
            var result = new List<YoutubeLiveChat>();
            foreach (var token in CommentsToken)
            {
                var comment = token["snippet"].ToObject<YoutubeLiveChat>();
                comment.CommentId = token["id"].ToObject<string>();
                result.Add(comment);
            }
            return result.ToArray();
        }
    }
}
