using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VtuberBot.Core.Entities
{
    public class BilibiliCommentInfo
    {
        [BsonElement("_id")]
        public ObjectId Id { get; set; }

        [BsonElement("publishTime")]
        public long PublishTime { get; set; }

        [BsonElement("masterId")]
        public long RoomMasterId { get; set; }

        [BsonElement("type")]
        public DanmakuType Type { get; set; }

        [BsonElement("suffix")]
        public string Suffix { get; set; }

        [BsonElement("suffixRoom")]
        public string SuffixRoom { get; set; }

        [BsonElement("suffixLevel")]
        public int SuffixLevel { get; set; }

        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("fromUsername")]
        public string Username { get; set; }

        [BsonElement("fromUserid")]
        public long Userid { get; set; }

        [BsonElement("giftName")]
        public string GiftName { get; set; }

        [BsonElement("giftCount")]
        public int GiftCount { get; set; }

        [BsonElement("costType")]
        public string CostType { get; set; }

        [BsonElement("cost")]
        public long Cost { get; set; }

        [BsonElement("isVip")]
        public bool IsVip { get; set; }

        [BsonElement("isAdmin")]
        public bool IsAdmin { get; set; }

        [BsonElement("popularity")]
        public int Popularity { get; set; }
    }

    public enum DanmakuType
    {
        Gift,
        Comment,
        JoinRoom
    }
}
