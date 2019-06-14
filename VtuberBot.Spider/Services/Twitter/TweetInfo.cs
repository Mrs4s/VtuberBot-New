using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VtuberBot.Spider.Services.Twitter
{
    public class TweetInfo
    {
        [JsonProperty("id")]
        [BsonElement("_id")]
        public long Id { get; set; }

        [JsonProperty("text")]
        [BsonElement("text")]
        public string Content { get; set; }

        [BsonIgnore]
        public DateTime CreateTime => DateTime.ParseExact(_createTime,
            "ddd MMM dd HH:mm:ss zzzz yyyy", CultureInfo.GetCultureInfo("en-us"));

        [JsonProperty("user")]
        [BsonElement("user")]
        public TwitterUser User { get; set; }

        [JsonProperty("is_quote_status")]
        [BsonElement("is_quote")]
        public bool IsQuote { get; set; }

        [JsonProperty("quoted_status")]
        [BsonElement("quoted_tweet")]
        public TweetInfo QuotedTweet { get; set; }

        [JsonProperty("retweeted_status")]
        [BsonElement("retweeted_tweet")]
        public TweetInfo RetweetedTweet { get; set; }

        [JsonIgnore]
        [BsonIgnore]
        public List<TweetMedia> MediaList => _entities?["media"]?.ToObject<List<TweetMedia>>();

        [BsonIgnore]
        public bool IsReply => _replyUserId != null;

        [BsonIgnore]
        public string ReplyScreenname => _replyScreenName;


        [JsonProperty("in_reply_to_status_id")]
        [BsonElement("replyTweetId")]
        private long? _replyTweetId;  //所回复的TweetId 可NULL 
        [JsonProperty("in_reply_to_user_id")]
        [BsonElement("replyUserId")]
        private long? _replyUserId;   //所回复的UserId 可NULL
        [JsonProperty("quoted_status_id")]
        [BsonElement("quotedTweetId")]
        private long? _quotedTweetId; //所引用的TweetId 可NULL
        [JsonProperty("in_reply_to_screen_name")]
        [BsonElement("replyScreenName")]
        private string _replyScreenName;  //所回复的Screenname 可NULL

        [JsonProperty("created_at")]
        [BsonElement("createTime")]
        private string _createTime;

        [JsonProperty("entities")]
        [BsonIgnore]
        private JToken _entities;


    }

    public class TweetMedia
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("media_url_https")]
        public string Url { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
