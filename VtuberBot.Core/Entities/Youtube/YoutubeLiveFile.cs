using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;

namespace VtuberBot.Core.Entities
{
    public class YoutubeLiveFile
    {
        [BsonElement("_id")]
        public string VideoId { get; set; }

        [BsonElement("fileHash")]
        public string FileHash { get; set; }

        [BsonElement("fileName")]
        public string FileName { get; set; }
    }
}
