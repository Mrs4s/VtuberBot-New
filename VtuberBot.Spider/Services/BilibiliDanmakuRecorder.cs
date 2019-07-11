using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using VtuberBot.Core;
using VtuberBot.Core.Entities;
using VtuberBot.Core.Extensions;
using VtuberBot.Spider.Services.Bilibili.Live;

namespace VtuberBot.Spider.Services
{
    public class BilibiliDanmakuRecorder
    {

        public VtuberEntity Vtuber { get; }


        public bool OnRecord { get; set; }

        public long RoomId { get; set; }

        public BilibiliLiveClient LiveClient { get; private set; }

        public event Action<BilibiliDanmakuRecorder> LiveStoppedEvent;


        private readonly IMongoCollection<BilibiliCommentInfo> _commentCollection;

        public BilibiliDanmakuRecorder(long roomId ,VtuberEntity vtuber)
        {
            RoomId = roomId;
            Vtuber = vtuber;
            _commentCollection = Program.Database.GetCollection<BilibiliCommentInfo>("bili-live-comments");
        }

        public async Task BeginRecordAsync()
        {
            LiveClient = new BilibiliLiveClient(RoomId);
            if (!await LiveClient.ConnectAsync())
            {
                LogHelper.Error("Cannot connect to danmaku server. roomId:"+RoomId);
                return;
            }
            OnRecord = true;
            LiveClient.GotDanmakuEvent += GotDanmakuEvent;
            LiveClient.GotGiftEvent += GotGiftEvent;
            LiveClient.SocketDisconnectEvent += client =>
            {
                if (OnRecord)
                    client.ConnectAsync().GetAwaiter().GetResult();
            };
            LiveClient.LiveStoppedEvent += client => LiveStoppedEvent?.Invoke(this);
        }

        public void StopRecord()
        {
            OnRecord = false;
            LiveClient.CloseConnect();
        }

        private async void GotGiftEvent(BilibiliGiftInfo gift)
        {
            var commentInfo = new BilibiliCommentInfo()
            {
                Id = ObjectId.GenerateNewId(DateTime.Now),
                PublishTime = DateTime.Now.ToTimestamp(),
                RoomMasterId = Vtuber.BilibiliUserId,
                Popularity = LiveClient.Popularity,
                Type = DanmakuType.Gift,
                Username = gift.Username,
                Userid = gift.Userid,
                GiftName = gift.GiftName,
                GiftCount = gift.Count,
                CostType = gift.CoinType,
                Cost = gift.CostCoin
            };
            try
            {
                await _commentCollection.InsertOneAsync(commentInfo);
            }
            catch (Exception ex)
            {
                LogHelper.Error("Insert object error.", ex: ex);
            }
        }

        private async void GotDanmakuEvent(BilibiliDanmakuInfo danmaku)
        {
            var commentInfo = new BilibiliCommentInfo()
            {
                Id = ObjectId.GenerateNewId(DateTime.Now),
                PublishTime = DateTime.Now.ToTimestamp(),
                RoomMasterId = Vtuber.BilibiliUserId,
                Popularity = LiveClient.Popularity,
                Type = DanmakuType.Comment,
                Suffix = danmaku.Suffix,
                SuffixRoom = danmaku.SuffixRoom,
                SuffixLevel = danmaku.SuffixLevel,
                Content = danmaku.Message,
                Username = danmaku.Username,
                Userid = danmaku.Userid,
                IsAdmin = danmaku.IsAdmin,
                IsVip = danmaku.IsVip
            };
            try
            {
                await _commentCollection.InsertOneAsync(commentInfo);
            }
            catch (Exception ex)
            {
                LogHelper.Error("Insert object error.", ex: ex);
            }
        }
    }
}
