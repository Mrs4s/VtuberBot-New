using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace VtuberBot.Spider.Services.Userlocal
{
    public class UserlocalVtuberInfo
    {
        [JsonProperty("youtube_key")]
        public string Id { get; set; }

        [JsonProperty("subject")]
        public string VtuberName { get; set; }

        [JsonProperty("tw_user_name")]
        public string TwitterProfileName { get; set; }

        [JsonProperty("reg_ts")]
        public DateTime RegisterTime { get; set; }

        [JsonProperty("channel_url")]
        public string ChannelLink { get; set; }

        [JsonProperty("office")]
        public string OfficeName { get; set; }

    }
}
