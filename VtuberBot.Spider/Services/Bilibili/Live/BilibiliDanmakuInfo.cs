using System;
using System.Collections.Generic;
using System.Text;

namespace VtuberBot.Spider.Services.Bilibili.Live
{
    public class BilibiliDanmakuInfo
    {
        public string Username { get; set; }

        public long Userid { get; set; }

        public string Suffix { get; set; }

        public string SuffixRoom { get; set; }

        public int SuffixLevel { get; set; }

        public string Message { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsVip { get; set; }
    }
}
