using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace VtuberBot.Spider
{
    public class Config
    {
        public static Config DefaultConfig => _defaultConfig ?? (_defaultConfig = LoadDefaultConfig());

        private static Config _defaultConfig;

        public string DatabaseUrl { get; set; }

        //<Callback url, Callback sign>
        public Dictionary<string, string> Callbacks { get; set; }

        public string YoutubeAccessToken { get; set; }

        public string TwitterAccessToken { get; set; }

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
}
