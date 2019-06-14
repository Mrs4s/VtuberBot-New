using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace VtuberBot.Spider.Services.Userlocal
{
    public class UserlocalSchedule
    {
        [JsonProperty("schedule_id")]
        public string ScheduleId { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("nickname")]
        public string VtuberName { get; set; }

        [JsonProperty("channel_url")]
        public string ChannelUrl { get; set; }

        [JsonProperty("scheduled_at")]
        public DateTime ScheduleTime { get; set; }

        [JsonProperty("office")]
        public string OfficeName { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }
}
