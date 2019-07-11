using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VtuberBot.Core.Extensions;

namespace VtuberBot.Core.Entities
{
    public class VtuberDatabaseEntity
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("accounts")]
        public JToken Accounts { get; set; }

        [JsonProperty("name")]
        public JToken Names { get; set; }

        [JsonIgnore]
        public string DefaultName => Names[Names["default"].ToString()].ToString();

        public string GetAccountIdByPlatform(string platform) =>
            Accounts.FirstOrDefault(v => v["platform"].ToString().EqualsIgnoreCase(platform))?["id"]?.ToString();
    }
}
