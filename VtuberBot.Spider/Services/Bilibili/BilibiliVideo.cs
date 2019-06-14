using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace VtuberBot.Spider.Services.Bilibili
{
    public class BilibiliVideo
    {
        [JsonProperty("aid")]
        public long Aid { get; set; }

        [JsonProperty("cid")]
        public long Cid { get; set; }

        [JsonProperty("ctime")]
        public long CreatedTime { get; set; }

        [JsonProperty("desc")]
        public string Description { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }
}
