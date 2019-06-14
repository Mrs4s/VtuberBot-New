using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace VtuberBot.Spider.Services.Youtube
{
    public class YoutubeVideo
    {
        public string Title { get; set; }

        public DateTime PublishTime { get; set; }

        public string VideoId { get; set; }

        public string VideoLink { get; set; }

        public string ChannelId { get; set; }

        public string ChannelTitle { get; set; }

        public string ThumbnailLink { get; set; }

        public string Description { get; set; }

        public bool IsLive { get; set; }

        public string WebLiveChatId { get; set; }

        

        public LiveStreamingDetail LiveDetails { get; set; }
    }

    public class LiveStreamingDetail
    {
        [JsonProperty("actualStartTime")]
        public DateTime? ActualStartTime { get; set; }

        [JsonProperty("actualEndTime")]
        public DateTime? ActualEndTime { get; set; }

        [JsonProperty("scheduledStartTime")]
        public DateTime ScheduledStartTime { get; set; }

        [JsonProperty("concurrentViewers")]
        public string ViewersCount { get; set; }

        [JsonProperty("activeLiveChatId")]
        public string LiveChatId { get; set; }
    }
}
