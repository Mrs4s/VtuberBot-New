using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace VtuberBot.Core.Entities.Bilibili
{
    public class BilibiliVideoInfo
    {
        [BsonElement("_id")]
        public long Cid { get; set; }

        [BsonElement("aid")]
        public long Aid { get; set; }

        [BsonElement("part")]
        public int PartNum { get; set; }

        [BsonElement("uploader")]
        public long Uploader { get; set; }

        [BsonElement("cover")]
        public string Cover { get; set; }

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("uploadTime")]
        public long CreatedTime { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("fileName")]
        public string FileName { get; set; }

        [BsonElement("fileHash")]
        public string FileHash { get; set; }


        [BsonIgnore]
        public string VideoLink => "https://www.bilibili.com/video/av" + Aid;

        public BilibiliVideoInfo Clone()
        {
            return new BilibiliVideoInfo()
            {
                Cid = Cid,
                Aid = Aid,
                PartNum = PartNum,
                Uploader = Uploader,
                Cover = Cover,
                Title = Title,
                CreatedTime = CreatedTime,
                Description = Description,
                FileName = FileName,
                FileHash = FileHash
            };
        }
    }
}
