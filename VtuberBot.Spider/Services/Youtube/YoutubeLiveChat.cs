using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VtuberBot.Spider.Services.Youtube
{
    [BsonIgnoreExtraElements]
    public class YoutubeLiveChat
    {
        [BsonElement("liveChatId")]
        [JsonProperty("liveChatId")]
        public string LiveChatId { get; set; }

        [BsonElement("videoId")]
        [JsonProperty("videoId")]
        public string VideoId { get; set; }

        [BsonElement("_id")]
        [JsonProperty("commentId")]
        public string CommentId { get; set; }

        [BsonElement("authorChannelId")]
        [JsonProperty("authorChannelId")]
        public string AuthorChannelId { get; set; }

        [BsonElement("publishedAt")]
        [JsonProperty("publishedAt")]
        public DateTime PublishTime { get; set; }

        [BsonElement("hasDisplayContent")]
        [JsonProperty("hasDisplayContent")]
        public bool HasDisplayContent { get; set; }

        [BsonElement("displayMessage")]
        [JsonProperty("displayMessage")]
        public string DisplayMessage { get; set; }

        [JsonProperty("textMessageDetails")]
        [BsonIgnore]
        public JToken TextMessageDetails { get; set; }

        [BsonIgnore]
        [JsonProperty("superChatDetails")]
        public JToken SuperChatDetails { get; set; }

        [BsonElement("viewerCount")]
        [JsonProperty("viewerCount")]
        public int ViewerCount { get; set; }


        [BsonElement("textMessageDetails")]
        [JsonIgnore]
        public string TextMessageDetailsJson
        {
            get => TextMessageDetails?.ToString(Formatting.None);
            set => TextMessageDetails = value == null ? null : JToken.Parse(value);
        }

        [BsonElement("superChatDetails")]
        [JsonIgnore]
        public string SuperChatDetailsJson
        {
            get => SuperChatDetails?.ToString(Formatting.None);
            set => SuperChatDetails = value == null ? null : JToken.Parse(value);
        }

        [JsonIgnore]
        [BsonIgnore]
        public bool IsSuperChat => SuperChatDetails != null && SuperChatDetails.HasValues;
    }
}
