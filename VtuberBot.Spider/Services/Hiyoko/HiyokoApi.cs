using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VtuberBot.Core.Extensions;

namespace VtuberBot.Spider.Services.Hiyoko
{
    public class HiyokoApi
    {
        public static List<HiyokoTimelineItem> GetLiveTimeline(string date)
        {
            using (var client = HttpClientExtensions.CreateClient())
            {
                var requestJson = new JObject()
                {
                    ["date"] = date,
                    ["user_token"] = null
                };
                return JsonConvert.DeserializeObject<List<HiyokoTimelineItem>>(
                    JObject.Parse(client
                            .PostJsonAsync("https://hiyoko.sonoj.net/f/avtapi/schedule/fetch_curr", requestJson)
                            .GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult())["schedules"]
                        .ToString());
            }

        }

        public static List<HiyokoStreamerInfo> SearchStreamer(string keyword)
        {
            using (var client=HttpClientExtensions.CreateClient())
            {
                var requestJson = new JObject()
                {
                    ["user_token"] = null,
                    ["keyword"] = keyword,
                    ["groups"] = string.Empty,
                    ["inc_old_group"] = 0,
                    ["retired"] = "all",
                    ["following"] = "all",
                    ["notifications"] = "all"
                };
                var json = JObject.Parse(client
                    .PostJsonAsync("https://hiyoko.sonoj.net/f/avtapi/search/streamer/fetch", requestJson).GetAwaiter()
                    .GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult());
                if (!json["result"].Any())
                    return new List<HiyokoStreamerInfo>();

                return json["result"]
                    .Select(token => JObject.Parse(client.PostJsonAsync(
                        "https://hiyoko.sonoj.net/f/avtapi/strm/fetch_summary", new
                        {
                            streamer_id = token["streamer_id"].ToObject<string>()
                        }).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult()))
                    .Select(infoJson => new HiyokoStreamerInfo
                    {
                        Channels = JsonConvert.DeserializeObject<HiyokoChannelInfo[]>(infoJson["channels"].ToString()),
                        Name = infoJson["streamer"]["name"].ToObject<string>(),
                        TwitterId = infoJson["streamer"]["twitter_id"].ToObject<string>()
                    }).ToList();
            }
        }
    }
}
