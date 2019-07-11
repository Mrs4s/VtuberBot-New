using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VtuberBot.Core.Entities;
using VtuberBot.Core.Extensions;

namespace VtuberBot.Spider.Services
{
    public static class VtuberDatabaseApi
    {
        public static async Task<VtuberDatabaseEntity[]> GetDatabaseEntitiesAsync()
        {
            using (var client = HttpClientExtensions.CreateClient())
            {
                var json = JToken.Parse(await client.GetStringAsync("https://vdb.vtbs.moe/json/list.json"));
                return json["vtbs"].ToObject<VtuberDatabaseEntity[]>();
            }
        }
    }
}
