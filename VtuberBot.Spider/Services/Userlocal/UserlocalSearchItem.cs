using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace VtuberBot.Spider.Services.Userlocal
{
    public class UserlocalSearchItem
    {
        [JsonProperty("youtube_key")]
        public string Id { get; set; }

        [JsonProperty("subject")]
        public string Name { get; set; }

        [JsonProperty("office")]
        public string OfficeName { get; set; }

        [JsonProperty("yt_id")]
        public string YoutubeChannelId { get; set; }
    }
}
