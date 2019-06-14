using System;
using System.Collections.Generic;
using System.Text;

namespace VtuberBot.Spider.Services.Twitcasting
{
    public class TwitcastingChannel
    {
        public string Username { get; set; }

        public string ChannelId { get; set; }

        public string LiveTitle { get; set; }

        public bool OnLive { get; set; }
    }
}
