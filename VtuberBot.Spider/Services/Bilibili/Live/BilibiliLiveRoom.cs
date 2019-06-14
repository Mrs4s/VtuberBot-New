using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using Newtonsoft.Json;

namespace VtuberBot.Spider.Services.Bilibili.Live
{
    public class BilibiliLiveRoom
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("uid")]
        public long Uid { get; set; }

        [JsonProperty("room_id")]
        public long RoomId { get; set; }

        [JsonProperty("cover")]
        public string CoverImage { get; set; }

        [JsonProperty("background")]
        public string BackgroundImage { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonIgnore]
        public bool OnLive => _liveStatus == 1;

        [JsonProperty("live_start_time")]
        public long LiveBeginTime { get; set; }

        [JsonProperty("up_session")]
        public string LiveSession { get; set; }



        [JsonProperty("live_status")]
        private int _liveStatus { get; set; }

    }
}
