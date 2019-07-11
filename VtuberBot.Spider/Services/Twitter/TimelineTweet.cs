using System;
using System.Collections.Generic;
using System.Text;

namespace VtuberBot.Spider.Services.Twitter
{
    public class TimelineTweet
    {
        public long Id { get; set; }

        public string ScreenName { get; set; }

        public bool Retweeted { get; set; }

        public string Content { get; set; }

        public bool Pinned { get; set; }
    }
}
