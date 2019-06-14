using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace VtuberBot.Spider.Services.Bilibili.Live
{
    public class BilibiliGiftInfo
    {
        [JsonProperty("giftName")]
        public string GiftName { get; set; }

        [JsonProperty("num")]
        public int Count { get; set; }

        [JsonProperty("uname")]
        public string Username { get; set; }

        [JsonProperty("uid")]
        public long Userid { get; set; }

        [JsonProperty("face")]
        public string FaceLink { get; set; }

        [JsonProperty("coin_type")]
        public string CoinType { get; set; }

        [JsonProperty("total_coin")]
        public long CostCoin { get; set; }
    }
}
