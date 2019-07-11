using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace VtuberBot.Core.Entities
{
    [BsonIgnoreExtraElements]
    public class VtuberEntity
    {

        [BsonElement("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

        [BsonElement("originalName")]
        public string OriginalName { get; set; }

        [BsonElement("chineseName")]
        public string ChineseName { get; set; }

        [BsonElement("twitcastingId")]
        public string TwitcastingId { get; set; }

        [BsonElement("face")]
        public string FaceLink { get; set; }

        [BsonElement("vdbUuid")]
        public string VtuberDatabaseUuid { get; set; }

        [BsonElement("bilibiliRoomId")]
        public long BilibiliLiveRoomId { get; set; }

        [BsonElement("youtubeChannelId")]
        public string YoutubeChannelId { get; set; }

        [BsonElement("twitterProfileId")]
        public string TwitterProfileId { get; set; }

        [BsonElement("bilibiliUid")]
        public long BilibiliUserId { get; set; }

        [BsonElement("userlocalProfileId")]
        public string UserlocalProfile { get; set; }

        [BsonElement("hiyokoProfileId")]
        public string HiyokoProfileId { get; set; }

        [BsonElement("groupName")]
        public string Group { get; set; }

        [BsonElement("youtubeUploadsPlaylistId")]
        public string YoutubeUploadsPlaylistId { get; set; }

        [BsonElement("recordLive")]
        public bool RecordLive { get; set; }

        [BsonElement("nickNameList")]
        public List<string> NickNames { get; set; } = new List<string>();


        public override int GetHashCode()
        {
            return OriginalName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VtuberEntity))
                return false;
            var target = (VtuberEntity) obj;
            return target.OriginalName == OriginalName;
        }
    }
}
