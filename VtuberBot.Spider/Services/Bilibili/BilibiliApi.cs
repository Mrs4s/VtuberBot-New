using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VtuberBot.Core.Extensions;
using VtuberBot.Spider.Services.Bilibili.Live;

namespace VtuberBot.Spider.Services.Bilibili
{
    public class BilibiliApi
    {
        private const string appSecret = "560c52ccd288fed045859ed18bffd973";
        private const string appKey = "1d8b6e7d45233436";

        public static async Task<BilibiliUser> GetBilibiliUserAsync(long userId)
        {
            using (var client = HttpClientExtensions.CreateClient(useGZip: true))
            {
                var json = JObject.Parse(await client.GetStringAsync($"https://api.bilibili.com/x/space/app/index?mid={userId}"));
                return json["data"]["info"].ToObject<BilibiliUser>();
            }
        }

        public static async Task<List<BilibiliUser>> SearchBilibiliUsersAsync(string keyword)
        {
            using (var client = HttpClientExtensions.CreateClient(useGZip: true))
            {
                var json = JToken.Parse(await client.GetStringAsync(
                    $"https://app.bilibili.com/x/v2/search/type?&build=12080&highlight=1&keyword={Uri.EscapeDataString(keyword)}&order=totalrank&order_sort=1&type=2"));
                if (json["data"].HasValues)
                {
                    var items = json["data"]["items"];
                    var users = items.Select(v => new BilibiliUser()
                    {
                        Username = v["title"]?.ToString(),
                        Id = int.Parse(v["param"]?.ToString()),
                        Description = v["sign"]?.ToString(),
                        Follower = v["fans"]?.ToObject<int>() ?? 0,
                        IsUploader = v["is_up"]?.ToObject<bool>() ?? false
                    });
                    return users.ToList();
                }
                return new List<BilibiliUser>();
            }
        }

        public static async Task<List<BilibiliDynamic>> GetDynamicsByUser(long userId)
        {
            using (var client = HttpClientExtensions.CreateClient(useGZip: true))
            {
                var json = JToken.Parse(await client.GetStringAsync(
                    "https://api.vc.bilibili.com/dynamic_svr/v1/dynamic_svr/space_history?host_uid=" + userId));
                if (json.Value<int>("code") != 0)
                    return new List<BilibiliDynamic>();
                return json["data"]["cards"].ToArray().Select(v => v.ToObject<BilibiliDynamic>()).ToList();
            }
        }

        public static async Task<BilibiliLiveRoom> GetLiveRoomAsync(long roomId)
        {
            using (var client = HttpClientExtensions.CreateClient())
            {
                var json = JToken.Parse(await client.GetStringAsync(Sign("https://api.live.bilibili.com/xlive/app-room/v1/index/getInfoByRoom", new {room_id = roomId})));
                return json["data"]?["room_info"]?.ToObject<BilibiliLiveRoom>();
            }
        }

        private static string Sign(string url, object requestParams)
        {
            var appParams = requestParams.GetType().GetRuntimeFields().ToDictionary(
                key => key.Name.Split('<').Last().Split('>').First(), value => value.GetValue(requestParams));
            appParams.Add("appkey", appKey); appParams.Add("actionKey", "appkey"); appParams.Add("device", "android");
            appParams.Add("platform", "android"); appParams.Add("build", 5410000); appParams.Add("ts", DateTime.Now.ToTimestamp());
            var str = string.Join("&", appParams.OrderBy(v => v.Key).Select(v => v.Key + "=" + v.Value.ToString()));
            var sign = (str + appSecret).ToMd5();
            return url + "?" + str + "&sign=" + sign;
        }
    }
}
