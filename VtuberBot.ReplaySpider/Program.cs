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

namespace VtuberBot.ReplaySpider
{
    public class Program
    {
        public static IMongoDatabase Database;

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Please input database url.");
                Console.ReadLine();
                return;
            }
            Database = new MongoClient(args[0]).GetDatabase("vtuber-bot-data");
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>().UseUrls("http://0.0.0.0:80");
    }
}
