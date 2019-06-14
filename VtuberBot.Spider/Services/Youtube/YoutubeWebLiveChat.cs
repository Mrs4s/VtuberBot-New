using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using VtuberBot.Core;
using VtuberBot.Core.Extensions;

namespace VtuberBot.Spider.Services.Youtube
{
    public class YoutubeWebLiveChat
    {
        [BsonElement("_id")]
        [JsonProperty("id")]
        public string Id { get; set; }

        [BsonElement("videoId")]
        [JsonProperty("videoId")]
        public string VideoId { get; set; }

        [BsonElement("liveChatId")]
        [JsonProperty("liveChatId")]
        public string LiveChatId { get; set; }

        [BsonElement("content")]
        [JsonProperty("content")]
        public string Content { get; set; }

        [BsonElement("publishTime")]
        [JsonIgnore]
        public DateTime PublishTime { get; set; }

        [BsonElement("authorName")]
        [JsonProperty("authorName")]
        public string AuthorName { get; set; }

        [BsonElement("authorChannelId")]
        [JsonProperty("authorChannelId")]
        public string AuthorChannelId { get; set; }

        [BsonElement("type")]
        [JsonProperty("contentType")]
        public YoutubeWebLiveChatType Type { get; set; }

        [BsonElement("costType")]
        [JsonProperty("amountType")]
        public string CostType { get; set; }

        [BsonElement("costAmount")]
        [JsonProperty("amount")]
        public float CostAmount { get; set; }

        [BsonIgnore]
        [JsonProperty("publishTime")]
        public long PublishTimestamp => PublishTime.ToTimestamp();


        public static (string, float) ParseWebSuperChat(string simpleText)
        {
            var dir = new Dictionary<string, string>()
            {
                ["$"] = "USD", ["£"] = "GBP", ["¥"] = "JPY", ["CA"] = "CAD",
                ["HK"] = "HKD", ["RUB"] = "RUB", ["NT$"] = "TWD", ["A$"] = "AUD",
                ["€"] = "EUR", ["₩"] = "KRW", ["PHP"] = "PHP", ["NZ"]="NZD",
                ["MX"] = "MXN"
            };
            var type = dir.FirstOrDefault(v => simpleText.StartsWith(v.Key)).Value;
            if (type == null)
            {
                LogHelper.Error("Cannot parse web super chat text " + simpleText);
                return (null, 0);
            }
            return (type, float.Parse(Regex
                .Match(simpleText.Replace(",", string.Empty), "\\d+(\\.\\d+){0,1}").Groups.First()
                .Value));
        }
    }

    public enum YoutubeWebLiveChatType
    {
        Chat,
        Superchat,
        Sponsor
    }
}
