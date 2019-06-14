using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;

namespace VtuberBot.Core.Entities
{
    [BsonIgnoreExtraElements]
    public class YoutubeLiveInfo
    {
        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("channelId")]
        public string Channel { get; set; }

        [BsonElement("beginTime")]
        public DateTime BeginTime { get; set; }

        [BsonElement("endTime")]
        public DateTime EndTime { get; set; }

        [BsonElement("superchatInfo")]
        public Dictionary<string,int> SuperchatInfo { get; set; }

        [BsonElement("exchangeRate")]
        public Dictionary<string,float> ExchangeRate { get; set; }

        [BsonElement("_id")]
        public string VideoId { get; set; }
    }
}
