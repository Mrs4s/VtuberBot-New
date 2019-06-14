using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VtuberBot.Spider.Services.Bilibili
{
    public class BilibiliUser
    {
        [JsonProperty("mid")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Username
        {
            get => _username;
            set => _username = value;
        }

        [JsonProperty("sign")]
        public string Description { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("following")]
        public int Following { get; set; }

        [JsonProperty("follower")]
        public int Follower { get; set; }

        [JsonIgnore]
        public bool IsUploader { get; set; }

        [JsonIgnore]
        public bool OnLive => (_liveInfo?["liveStatus"]?.ToObject<int>() ?? 0) == 1;

        [JsonIgnore]
        public string LiveUrl => _liveInfo?["url"]?.ToObject<string>();

        [JsonIgnore]
        public string LiveTitle => _liveInfo?["title"]?.ToObject<string>();

        [JsonIgnore]
        public long LiveRoomId => _liveInfo?["roomid"]?.ToObject<long>() ?? 0;

        [JsonProperty("live")]
        private JToken _liveInfo;

        [JsonProperty("uname")]
        private string _username;
    }
}
