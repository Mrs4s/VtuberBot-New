using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using VtuberBot.Core;
using VtuberBot.Core.Entities;
using VtuberBot.Core.Entities.Bilibili;
using VtuberBot.Core.Extensions;
using VtuberBot.Core.Tools;
using VtuberBot.Spider.Services;
using VtuberBot.Spider.Services.Bilibili;
using VtuberBot.Spider.Services.Bilibili.Live;
using VtuberBot.Spider.Services.Twitter;
using VtuberBot.Spider.Services.Youtube;

namespace VtuberBot.Spider
{
    public class CacheManager
    {
        public static CacheManager Manager { get; } = new CacheManager();

        private readonly IMongoCollection<VtuberEntity> _vtuberCollection;
        private readonly IMongoCollection<YoutubeLiveInfo> _youtubeLiveCollection;
        private readonly IMongoCollection<BilibiliLiveInfo> _biliLiveCollection;
        private readonly IMongoCollection<TweetInfo> _tweetCollection;
        //private readonly IMongoCollection<BiliBiliDynamic> _dynamicCollection;


        private CacheManager()
        {
            _vtuberCollection = Program.Database.GetCollection<VtuberEntity>("vtubers");
            _youtubeLiveCollection = Program.Database.GetCollection<YoutubeLiveInfo>("youtube-live-details");
            _tweetCollection = Program.Database.GetCollection<TweetInfo>("tweet-details");
            _biliLiveCollection = Program.Database.GetCollection<BilibiliLiveInfo>("bili-live-details");
        }

        #region Caches
        public Dictionary<VtuberEntity, List<YoutubeVideo>> LastCheckYoutubeVideos { get; } =
            new Dictionary<VtuberEntity, List<YoutubeVideo>>();

        public Dictionary<VtuberEntity, YoutubeVideo> LastCheckYoutubeLiveStatus { get; } = new Dictionary<VtuberEntity, YoutubeVideo>();

        public Dictionary<VtuberEntity, List<TweetInfo>> LastCheckTweets { get; } =
            new Dictionary<VtuberEntity, List<TweetInfo>>();

        public Dictionary<VtuberEntity, DateTime> LastTweetTime { get; } = new Dictionary<VtuberEntity, DateTime>();

        public Dictionary<VtuberEntity, DateTime> LastReplyTime { get; } = new Dictionary<VtuberEntity, DateTime>();

        public Dictionary<VtuberEntity, DateTime> LastRetweetedTime { get; } = new Dictionary<VtuberEntity, DateTime>();

        public Dictionary<VtuberEntity, BilibiliLiveRoom> LastCheckBilibiliLive { get; } =
            new Dictionary<VtuberEntity, BilibiliLiveRoom>();

        public Dictionary<VtuberEntity, BilibiliDynamic> LastCheckBilibiliDynamic { get; } =
            new Dictionary<VtuberEntity, BilibiliDynamic>();

        public Dictionary<VtuberEntity, BilibiliDanmakuRecorder> BilibiliRecorders { get; } =
            new Dictionary<VtuberEntity, BilibiliDanmakuRecorder>();

        #endregion

        public event Action<VtuberEntity, YoutubeVideo> VtuberBeginYoutubeLiveEvent;
        public event Action<VtuberEntity, YoutubeVideo> VtuberUploadYoutubeVideoEvent;
        public event Action<VtuberEntity, YoutubeVideo> VtuberStoppedYoutubeLiveEvent;
        public event Action<VtuberEntity, VtuberEntity, YoutubeLiveChat, YoutubeVideo> VtuberCommentedYoutubeLiveEvent; //评论者 目标Vtuber 评论内容 视频 
        public event Action<VtuberEntity, BilibiliLiveRoom> VtuberBeginBilibiliLiveEvent;
        public event Action<VtuberEntity, BilibiliLiveRoom> VtuberStoppedBilibiliLiveEvent;
        public event Action<VtuberEntity, TweetInfo> VtuberPublishTweetEvent;
        public event Action<VtuberEntity, TweetInfo> VtuberReplyTweetEvent;
        public event Action<VtuberEntity, TweetInfo> VtuberRetweetedEvent;

        public void Init()
        {
            //Twitter Dynamic Userlocal check task.
            new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(1000 * 60);
                        var vtuberList = (await _vtuberCollection.FindAsync(v => true)).ToList();
                        await TwitterCheckTimer(vtuberList);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error("Timer error", true, ex);
                    }
                }
            })
            { IsBackground = true }.Start();

            //Youtube live check task
            new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        Thread.Sleep(1000 * 5);
                        var vtuberList = (_vtuberCollection.FindAsync(v => true).GetAwaiter().GetResult()).ToList();
                        LogHelper.Info($"开始爬取 {vtuberList.Count} 个Vtuber的Youtube主页.", false);
                        YoutubeLiveCheckTimer(vtuberList);
                        LogHelper.Info("爬取完成");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error("Timer error", true, ex);
                    }
                }
            })
            { IsBackground = true }.Start();

            //Bilibili about check task
            new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(1000 * 30);
                        LogHelper.Info("开始爬取B站相关信息");
                        var vtuberList = (await _vtuberCollection.FindAsync(v => true)).ToList();
                        await BilibiliLiveCheckTimer(vtuberList);
                        LogHelper.Info("爬取完成.");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error("Timer error", ex: ex);
                    }
                }
            })
            { IsBackground = true }.Start();
        }

        private async Task TwitterCheckTimer(IEnumerable<VtuberEntity> vtubers)
        {
            foreach (var vtuber in vtubers.Where(v => !string.IsNullOrEmpty(v.TwitterProfileId)))
            {
                var timeline = await TwitterApi.GetTimelineByUserAsync(vtuber.TwitterProfileId, 10);
                if (timeline == null)
                    continue;
                if (!LastCheckTweets.ContainsKey(vtuber))
                    LastCheckTweets.Add(vtuber, timeline);
                LastCheckTweets[vtuber] = timeline;
                var lastRetweeted = timeline.FirstOrDefault(v => v.RetweetedTweet != null);
                var lastPublish = timeline.FirstOrDefault(v => v.RetweetedTweet == null && !v.IsReply && !v.IsQuote);
                var lastReply = timeline.FirstOrDefault(v => v.IsReply);
                //Update cache
                if (lastPublish != null)
                {
                    if (!LastTweetTime.ContainsKey(vtuber))
                        LastTweetTime.Add(vtuber, lastPublish.CreateTime);
                    if (LastTweetTime[vtuber] != lastPublish.CreateTime)
                    {
                        LastTweetTime[vtuber] = lastPublish.CreateTime;
                        await _tweetCollection.InsertOneAsync(lastPublish);
                        VtuberPublishTweetEvent?.Invoke(vtuber, lastPublish);
                    }
                }

                if (lastReply != null)
                {
                    if (!LastReplyTime.ContainsKey(vtuber))
                        LastReplyTime.Add(vtuber, lastReply.CreateTime);
                    if (LastReplyTime[vtuber] != lastReply.CreateTime)
                    {
                        LastReplyTime[vtuber] = lastReply.CreateTime;
                        await _tweetCollection.InsertOneAsync(lastReply);
                        VtuberReplyTweetEvent?.Invoke(vtuber, lastReply);
                    }
                }

                if (lastRetweeted != null)
                {
                    if (!LastRetweetedTime.ContainsKey(vtuber))
                        LastRetweetedTime.Add(vtuber, lastRetweeted.CreateTime);
                    if (LastRetweetedTime[vtuber] != lastRetweeted.CreateTime)
                    {
                        LastRetweetedTime[vtuber] = lastRetweeted.CreateTime;
                        await _tweetCollection.InsertOneAsync(lastRetweeted);
                        VtuberRetweetedEvent?.Invoke(vtuber, lastRetweeted);
                    }
                }
            }
        }

        private void YoutubeLiveCheckTimer(IEnumerable<VtuberEntity> vtubers)
        {
            var threadPool = new SimpleThreadPool()
            {
                MaxThread = 20
            };
            foreach (var vtuber in vtubers.Where(v => !string.IsNullOrEmpty(v.YoutubeChannelId)))
            {
                threadPool.Actions.Enqueue(() =>
               {
                   if (!LastCheckYoutubeLiveStatus.ContainsKey(vtuber))
                       LastCheckYoutubeLiveStatus.Add(vtuber, new YoutubeVideo());
                   var onLive = YoutubeApi.NowLive(vtuber.YoutubeChannelId).GetAwaiter().GetResult();
                   if (onLive)
                   {
                       var channelInfo = YoutubeApi.GetYoutubeChannelAsync(vtuber.YoutubeChannelId).GetAwaiter().GetResult();
                       if (!LastCheckYoutubeLiveStatus[vtuber].IsLive || channelInfo.LiveVideoId != LastCheckYoutubeLiveStatus[vtuber].VideoId)
                       {
                           var live = YoutubeApi.GetYoutubeVideoAsync(channelInfo.LiveVideoId).GetAwaiter().GetResult();
                           if (!live.IsLive)
                               return;
                           if (!LastCheckYoutubeLiveStatus[vtuber].IsLive)
                           {
                               LastCheckYoutubeLiveStatus[vtuber] = live;
                               var recorder = new YoutubeLiveChatRecorder(live.LiveDetails.LiveChatId, vtuber, live.VideoId) { VtuberList = vtubers.ToList() };
                               recorder.StartRecord();
                               recorder.LiveStoppedEvent += (id, sender) =>
                               {
                                   LogHelper.Info($"{vtuber.OriginalName} 已停止直播，正在保存评论数据");
                                   live = YoutubeApi.GetYoutubeVideoAsync(channelInfo.LiveVideoId).Retry(5).GetAwaiter().GetResult() ?? live;
                                   var info = new YoutubeLiveInfo()
                                   {
                                       Title = live.Title,
                                       Channel = live.ChannelId,
                                       BeginTime = live.LiveDetails?.ActualStartTime ?? default(DateTime),
                                       EndTime = live.LiveDetails?.ActualEndTime ?? DateTime.Now,
                                       VideoId = live.VideoId
                                   };
                                   _youtubeLiveCollection.ReplaceOne(v => v.VideoId == live.VideoId, info,
                                       new UpdateOptions() { IsUpsert = true });
                                   LogHelper.Info("保存完毕");
                                   VtuberStoppedYoutubeLiveEvent?.Invoke(vtuber, live);
                               };
                               recorder.VtuberCommentedEvent += (author, message, sender) => VtuberCommentedYoutubeLiveEvent?.Invoke(author, sender.Vtuber, message, live);
                               VtuberBeginYoutubeLiveEvent?.Invoke(vtuber, live);
                           }
                       }
                   }
                   if (!onLive)
                       LastCheckYoutubeLiveStatus[vtuber] = new YoutubeVideo();
               });
            }
            threadPool.Run();
        }

        private async Task BilibiliLiveCheckTimer(IEnumerable<VtuberEntity> vtubers)
        {
            foreach (var vtuberEntity in vtubers.Where(v => v.BilibiliUserId != 0 && v.BilibiliLiveRoomId != -1))
            {
                if (!LastCheckBilibiliLive.ContainsKey(vtuberEntity))
                    LastCheckBilibiliLive.Add(vtuberEntity, new BilibiliLiveRoom());
                if (vtuberEntity.BilibiliLiveRoomId == 0)
                {
                    var userInfo = await BilibiliApi.GetBilibiliUserAsync(vtuberEntity.BilibiliUserId);
                    vtuberEntity.BilibiliLiveRoomId = userInfo.LiveRoomId == 0 ? -1 : userInfo.LiveRoomId;
                    _vtuberCollection.ReplaceOne(v => v.Id == vtuberEntity.Id, vtuberEntity,
                        new UpdateOptions() { IsUpsert = true });
                    LogHelper.Info($"更新Vtuber {vtuberEntity.OriginalName} 的Live room id: {userInfo.LiveRoomId}");
                }
                if (vtuberEntity.BilibiliLiveRoomId <= 0)
                    continue;
                if (!BilibiliRecorders.ContainsKey(vtuberEntity) && vtuberEntity.BilibiliLiveRoomId > 0)
                {
                    BilibiliRecorders.Add(vtuberEntity,
                        new BilibiliDanmakuRecorder(vtuberEntity.BilibiliLiveRoomId, vtuberEntity));
                    LogHelper.Info($"开始监听Vtuber {vtuberEntity.OriginalName} 的B站直播间.");
                    await BilibiliRecorders[vtuberEntity].BeginRecordAsync();
                }
                var roomInfo = await BilibiliApi.GetLiveRoomAsync(vtuberEntity.BilibiliLiveRoomId).Retry(3);
                if (roomInfo == null)
                    continue;
                if (roomInfo.OnLive && !LastCheckBilibiliLive[vtuberEntity].OnLive)
                {
                    var liveId = roomInfo.LiveSession;
                    var beginTime = DateTimeExtensions.TimestampToDateTime(roomInfo.LiveBeginTime);
                    void OnLiveStoppedEvent(BilibiliDanmakuRecorder client)
                    {
                        LogHelper.Info($"{vtuberEntity.OriginalName} 已停止在B站的直播");
                        var endTime = DateTime.Now;
                        var info = new BilibiliLiveInfo()
                        {
                            LiveId = liveId,
                            BeginTime = beginTime,
                            EndTime = endTime,
                            ChannelId = vtuberEntity.BilibiliUserId,
                            MaxPopularity = client.LiveClient.MaxPopularity,
                            Title = roomInfo.Title
                        };
                        _biliLiveCollection.InsertOneAsync(info).Retry(5).GetAwaiter().GetResult();
                        VtuberStoppedBilibiliLiveEvent?.Invoke(vtuberEntity, roomInfo);
                        BilibiliRecorders[vtuberEntity].LiveStoppedEvent -= OnLiveStoppedEvent;
                    }
                    BilibiliRecorders[vtuberEntity].LiveStoppedEvent += OnLiveStoppedEvent;
                    VtuberBeginBilibiliLiveEvent?.Invoke(vtuberEntity, roomInfo);
                }
                LastCheckBilibiliLive[vtuberEntity] = roomInfo;
            }
        }

        private async Task BilibiliDynamicsCheckTimer(IEnumerable<VtuberEntity> vtubers)
        {
            foreach (var vtuberEntity in vtubers.Where(v => v.BilibiliUserId != 0))
            {
                var timeline = await BilibiliApi.GetDynamicsByUser(vtuberEntity.BilibiliUserId);
                if (timeline?.Any() ?? false)
                    continue;
                if (!LastCheckBilibiliDynamic.ContainsKey(vtuberEntity))
                    LastCheckBilibiliDynamic.Add(vtuberEntity, timeline.First());

            }
        }

    }
}
