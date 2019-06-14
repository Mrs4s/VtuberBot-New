using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VtuberBot.Core.Extensions;

namespace VtuberBot.Spider.Services.Userlocal
{
    public class UserlocalApi
    {
        public static async Task<List<UserlocalSchedule>> GetSchedulesAsync()
        {
            using (var client = HttpClientExtensions.CreateClient(useGZip: true))
            {
                var json = JToken.Parse(
                    await client.GetStringAsync(
                        "https://social.userlocal.jp/apis/data/youtube_vy_live_program.php?output=json"));
                return json.ToObject<List<UserlocalSchedule>>();
            }
        }

        public static async Task<UserlocalVtuberInfo> GetVtuberInfoAsync(string id)
        {
            using (var client = HttpClientExtensions.CreateClient(useGZip: true))
            {
                var json = JToken.Parse(
                    await client.GetStringAsync(
                        $"https://social.userlocal.jp/apis/data/youtube.php?user_name={id}&output=json"));
                return json.ToObject<UserlocalVtuberInfo>();
            }
        }

        public static async Task<List<UserlocalSearchItem>> SearchAsync(string keyword)
        {
            using (var client = HttpClientExtensions.CreateClient(useGZip: true))
            {
                var json = JToken.Parse(await client.GetStringAsync(
                    "https://social.userlocal.jp/apis/data/youtube_vy_search.php?output=json&q=" +
                    keyword.UrlEncode()));
                return json.ToObject<List<UserlocalSearchItem>>();
            }
        }
    }
}
