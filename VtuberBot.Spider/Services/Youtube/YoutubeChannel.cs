using System;
using System.Collections.Generic;
using System.Text;

namespace VtuberBot.Spider.Services.Youtube
{
    public class YoutubeChannel
    {
        public string ChannelName { get; set; }

        public string ChannelId { get; set; }

        public string Face { get; set; }

        public string Description { get; set; }

        public string[] Tags { get; set; }

        public int SubscriberCount { get; set; }

        public bool NowLive { get; set; }

        public string LiveVideoId { get; set; }

        public int LiveViewerCount { get; set; }
    }
}
