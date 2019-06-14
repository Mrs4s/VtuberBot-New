using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VtuberBot
{
    public class Config
    {
        public static Config DefaultConfig => _defaultConfig ?? (_defaultConfig = LoadDefaultConfig());

        private static Config _defaultConfig;

        public string DatabaseUrl { get; set; }

        public string CallbackSign { get; set; } = "default sign";

        public string ListenUrl { get; set; } = "http://0.0.0.0:2998";

        public List<GroupConfig> GroupConfigs { get; set; } = new List<GroupConfig>();

        public List<BotServiceClient> Clients { get; set; } = new List<BotServiceClient>()
        {
            new BotServiceClient()
            {
                ClientId = "default client",
                Services = new []
                {
                    new BotServiceConfig()
                    {
                        ServiceType = "LightQQ",
                        AccessToken = "your access token",
                        ListenPort = 0,
                        ListenUrl = "http://127.0.0.1/bot",
                        WsUrl = "ws://127.0.0.1/bot"
                    },
                }
            }
        };


        public static Config LoadDefaultConfig()
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Config.json")))
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Config.json"),
                    JsonConvert.SerializeObject(new Config()));
            return
                JsonConvert.DeserializeObject<Config>(
                    File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Config.json")));
        }
        public static void SaveToDefaultFile(Config config)
        {
            lock (config)
            {
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Config.json"),
                    JsonConvert.SerializeObject(config, Formatting.Indented));
            }
        }
    }

    public class BotServiceClient
    {
        public string ClientId { get; set; }

        public BotServiceConfig[] Services { get; set; }
    }

    public class BotServiceConfig
    {
        public int Level { get; set; }

        public string ServiceType { get; set; }

        public string ListenUrl { get; set; }

        public string WsUrl { get; set; }

        public int ListenPort { get; set; }

        public string AccessToken { get; set; }
    }

    public class GroupConfig
    {
        public long GroupId { get; set; }

        public bool PrePublish { get; set; }

        public List<string> Permissions { get; set; } = new List<string>();

        public List<VtuberPublishConfig> PublishConfigs { get; set; }
    }

    public class VtuberPublishConfig
    {
        public string VtuberName { get; set; }

        public bool PublishTweet { get; set; }

        public bool TweetImage { get; set; }

        public bool ReplyTweet { get; set; }

        public bool Retweeted { get; set; }

        public bool YoutubeBeginLive { get; set; }

        public bool YoutubeComment { get; set; }

        public bool BilibiliBeginLive { get; set; }

        public bool BilibiliPublishDynamic { get; set; }

        public bool BilibiliUploadVideo { get; set; }

        public bool YoutubeUploadVideo { get; set; }
    }
}
