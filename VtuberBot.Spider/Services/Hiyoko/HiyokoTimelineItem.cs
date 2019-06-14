using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace VtuberBot.Spider.Services.Hiyoko
{
    public class HiyokoTimelineItem
    {

        [JsonProperty("ch_id")]
        public string ChannelId { get; set; }

        [JsonProperty("ch_type")]
        public int ChannelType { get; set; }

        [JsonProperty("scheduled_start_time")]
        public long StartTime { get; set; }

        [JsonProperty("streamer_id")]
        public string StreamerId { get; set; }

        [JsonProperty("streamer_name")]
        public string StreamerName { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("video_id")]
        public string VideoId { get; set; }

    }
}
