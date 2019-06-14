using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VtuberBot.Spider.Services.Bilibili
{
    public class BilibiliDynamic
    {

        [BsonElement("_id")]
        public string Id
        {
            get => Info.Id.ToString();
            set => Info.Id = long.Parse(value);
        }


        [JsonProperty("desc")]
        [BsonElement("info")]
        public DynamicInfo Info { get; set; }

        [JsonProperty("card")]
        [BsonElement("cardJson")]
        public string CardJson { get; set; }

        [JsonProperty("extend_json")]
        [BsonElement("extendJson")]
        public string ExtendJson { get; set; }

        /// <summary>
        /// Get dynamic card info.
        /// Cannot get video info.
        /// </summary>
        [JsonIgnore]
        [BsonIgnore]
        public DynamicCard CardInfo => JsonConvert.DeserializeObject<DynamicCard>(CardJson);

        /// <summary>
        /// Get dynamic video info.
        /// </summary>
        [JsonIgnore]
        [BsonIgnore]
        public BilibiliVideo VideoInfo => JsonConvert.DeserializeObject<BilibiliVideo>(CardJson);
    }

    public class DynamicCard
    {
        public string Content => _itemToken["content"]?.ToString() ?? _itemToken["description"].ToString();

        public long TimeStamp => _itemToken["timestamp"]?.ToObject<long>() ?? _itemToken["upload_time"].ToObject<long>();

        public string RedynamicUsername => _originUserToken?["info"]["uname"].ToString();

        public long RedynamicUserid => _originUserToken?["info"]["uid"].ToObject<long>() ?? 0;

        public string[] Pictures => _itemToken["pictures"]?.Select(v => v["img_src"].ToString()).ToArray();

        [JsonProperty("origin")]
        public string OriginJson { get; set; }


        [JsonProperty("item")]
        private JToken _itemToken;

        [JsonProperty("origin_user")]
        private JToken _originUserToken;

    }

    public class DynamicInfo
    {
        [JsonProperty("uid")]
        [BsonElement("createdBy")]
        public long CreatedBy { get; set; }

        [JsonProperty("dynamic_id")]
        [BsonElement("dynamic_id")]
        public long Id { get; set; }

        [JsonProperty("type")]
        [BsonElement("type")]
        public DynamicType Type { get; set; }

        [JsonProperty("rid")]
        [BsonElement("redynamicId")]
        public long RedynamicId { get; set; }

        [JsonProperty("status")]
        [BsonElement("status")]
        public int Status { get; set; }

    }

    public enum DynamicType
    {
        ReDynamic = 1,
        TextAndMedia = 2,
        TextNotMedia = 4,
        Video = 8,
        ShortVideo = 16
    }
}
