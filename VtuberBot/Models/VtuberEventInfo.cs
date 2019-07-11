using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VtuberBot.Models
{
    public class VtuberEventInfo
    {
        public VtuberEventType Type { get; set; }
        
        public string AuthorName { get; set; }

        public string Url { get; set; }

        public string Content { get; set; }

        public DateTime Time { get; set; }

        public string Info { get; set; }
    }

    public enum VtuberEventType
    {
        BeginningYoutubeLive = 1,
        EndYoutubeLive,
        PublishedYoutubeLiveChat,
        PublishedTweet,
        Retweeted,
        ReplyTweet,
    }
}
