using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using VtuberBot.Core.Entities.Bilibili;

namespace VtuberBot.VideoDownloader
{
    public class Program
    {

        public static IMongoCollection<BilibiliVideoInfo> VideoCollection { get; private set; }

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Please input database url.");
                Console.ReadKey();
                return;
            }
            var db = new MongoClient(args[0]);
            VideoCollection = db.GetDatabase("vtuber-bot-data").GetCollection<BilibiliVideoInfo>("bili-video-details");
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>().UseUrls("http://0.0.0.0:80");
    }
}
