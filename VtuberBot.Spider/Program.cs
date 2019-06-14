using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using VtuberBot.Core;
using VtuberBot.Core.Entities;
using VtuberBot.Core.Extensions;
using VtuberBot.Spider.Services;
using VtuberBot.Spider.Services.Bilibili;
using VtuberBot.Spider.Services.Twitter;
using VtuberBot.Spider.Services.Youtube;

namespace VtuberBot.Spider
{
    class Program
    {
        public static IMongoDatabase Database { get; set; }

        public static List<VtuberBotCallbackApi> CallbackApis { get; set; } = new List<VtuberBotCallbackApi>();

        static void Main(string[] args)
        {
            LogHelper.Info("初始化爬虫...");
            ServicePointManager.DefaultConnectionLimit = 9999;
            Database = new MongoClient(Config.DefaultConfig.DatabaseUrl).GetDatabase("vtuber-bot-data");
            CallbackApis = Config.DefaultConfig.Callbacks.Select(v => new VtuberBotCallbackApi(v.Key, v.Value))
                .ToList();
            CacheManager.Manager.Init();
            CacheManager.Manager.VtuberBeginYoutubeLiveEvent += VtuberBeginYoutubeLiveEvent;
            CacheManager.Manager.VtuberStoppedYoutubeLiveEvent += VtuberStoppedYoutubeLiveEvent;
            CacheManager.Manager.VtuberUploadYoutubeVideoEvent += VtuberUploadYoutubeVideoEvent;
            CacheManager.Manager.VtuberCommentedYoutubeLiveEvent += VtuberCommentedYoutubeLiveEvent;
            CacheManager.Manager.VtuberBeginBilibiliLiveEvent += VtuberBeginBilibiliLiveEvent;
            CacheManager.Manager.VtuberPublishTweetEvent += VtuberPublishTweetEvent;
            CacheManager.Manager.VtuberReplyTweetEvent += VtuberReplyTweetEvent;
            CacheManager.Manager.VtuberRetweetedEvent += VtuberRetweetedEvent;
            LogHelper.Info("初始化完成");
            while (true)
            {
                Thread.Sleep(60000); //1MIN
                var lives = CacheManager.Manager.LastCheckYoutubeLiveStatus.ToArray().Where(v => v.Value.IsLive);
                foreach (var (key, value) in lives)
                {
                    foreach (var api in CallbackApis)
                        api.CallYoutubeBeginLiveAsync(key, value).GetAwaiter().GetResult();
                }
            }
        }

        private static async void VtuberCommentedYoutubeLiveEvent(VtuberEntity author, VtuberEntity target, YoutubeLiveChat message, YoutubeVideo video)
        {
            LogHelper.Info($"Vtuber [{author.OriginalName}] 在 [{target.OriginalName}] 的Youtube直播 {video.VideoLink} 中发布了评论: {message.DisplayMessage}");
            foreach (var api in CallbackApis)
                await api.CallYoutubeCommentedAsync(author, target, message);
        }

        private static async void VtuberBeginBilibiliLiveEvent(VtuberEntity vtuber, BilibiliUser user)
        {
            LogHelper.Info($"Vtuber [{vtuber.OriginalName}] 在B站开始了直播 {user.LiveTitle}");
            foreach (var api in CallbackApis)
                await api.CallBilibiliBeginLiveAsync(vtuber, user);
        }

        private static async void VtuberStoppedYoutubeLiveEvent(VtuberEntity vtuber, YoutubeVideo liveVideo)
        {
            LogHelper.Info($"Vtuber [{vtuber.OriginalName}] 结束了直播.");
            foreach (var api in CallbackApis)
                await api.CallYoutubeStopLiveAsync(vtuber, liveVideo);
        }

        private static async void VtuberRetweetedEvent(VtuberEntity vtuber, TweetInfo tweet)
        {
            LogHelper.Info($"Vtuber [{vtuber.OriginalName}] 转发了 {tweet.RetweetedTweet.User.Name} 的推特: {tweet.Content}");
            foreach (var api in CallbackApis)
                await api.CallRetweetedAsync(vtuber, tweet);
        }

        private static async void VtuberReplyTweetEvent(VtuberEntity vtuber, TweetInfo tweet)
        {
            LogHelper.Info($"Vtuber [{vtuber.OriginalName}] 回复了 {tweet.ReplyScreenname} 的推特 {tweet.Content}");
            foreach (var api in CallbackApis)
                await api.CallReplyAsync(vtuber, tweet);
        }

        private static async void VtuberPublishTweetEvent(VtuberEntity vtuber, TweetInfo tweet)
        {
            LogHelper.Info($"Vtuber [{vtuber.OriginalName}] 发布了新的推特 {tweet.Content}");
            foreach (var api in CallbackApis)
                await api.CallPublishTweetAsync(vtuber, tweet);
        }

        private static void VtuberUploadYoutubeVideoEvent(VtuberEntity vtuber, YoutubeVideo video)
        {

        }

        private static async void VtuberBeginYoutubeLiveEvent(VtuberEntity vtuber, YoutubeVideo liveVideo)
        {
            LogHelper.Info($"Vtuber [{vtuber.OriginalName}] 在Youtube开始了直播 {liveVideo.VideoLink} ({liveVideo.Title})");
            foreach (var api in CallbackApis)
                await api.CallYoutubeBeginLiveAsync(vtuber, liveVideo);
        }
    }
}
