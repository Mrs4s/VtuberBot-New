using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MongoDB.Driver;
using VtuberBot.Core.Entities;
using VtuberBot.Core.Extensions;
using VtuberBot.Spider.Services.Youtube;

namespace VtuberBot.Spider.Services
{
    public class YoutubeLiveChatRecorder
    {
        public string LiveChatId { get; }

        public string VideoId { get; }

        public VtuberEntity Vtuber { get; set; }

        public List<VtuberEntity> VtuberList { get; set; }


        public List<YoutubeLiveChat> RecordedLiveChats { get; } = new List<YoutubeLiveChat>();

        private readonly IMongoCollection<YoutubeLiveChat> _chatCollection;


        public event Action<string, YoutubeLiveChatRecorder> LiveStoppedEvent;
        public event Action<VtuberEntity, YoutubeLiveChat, YoutubeLiveChatRecorder> VtuberCommentedEvent;

        public YoutubeLiveChatRecorder(string liveChatId, VtuberEntity vtuber, string videoId)
        {
            LiveChatId = liveChatId;
            Vtuber = vtuber;
            VideoId = videoId;
            _chatCollection = Program.Database.GetCollection<YoutubeLiveChat>("youtube-live-chats");
        }


        public void StartRecord()
        {
            var num = 0;
            new Thread(() =>
            {
                while (true)
                {
                    var info = YoutubeApi.GetLiveChatInfoAsync(LiveChatId).GetAwaiter().GetResult();
                    if (info == null || (info.PollingInterval == 0 && info.CommentsToken == null))
                    {
                        LiveStoppedEvent?.Invoke(LiveChatId, this);
                        break;
                    }
                    var channelInfo = YoutubeApi.GetYoutubeChannelAsync(Vtuber.YoutubeChannelId).Retry(5).GetAwaiter()
                        .GetResult();
                    var comments = info.GetComments().Where(comment => RecordedLiveChats.All(v => v.CommentId != comment.CommentId)).ToList();
                    if (channelInfo != null && channelInfo.LiveViewerCount > 0)
                        comments.ForEach(v => v.ViewerCount = channelInfo.LiveViewerCount);
                    if (comments.Any())
                    {
                        comments.ForEach(v => v.VideoId = VideoId);
                        try
                        {
                            _chatCollection.InsertMany(comments, new InsertManyOptions()
                            {
                                IsOrdered = false
                            });
                            RecordedLiveChats.AddRange(comments);
                            if (VtuberList != null)
                            {
                                var comment = comments.Where(v => VtuberList.Any(t => t.YoutubeChannelId == v.AuthorChannelId));
                                foreach (var chat in comment)
                                {
                                    var target = VtuberList.First(v => v.YoutubeChannelId == chat.AuthorChannelId);
                                    VtuberCommentedEvent?.Invoke(target, chat, this);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(10000);
                        }
                    }
                    if (++num % 10 == 0)
                    {
                        if (RecordedLiveChats.Count > 100)
                            RecordedLiveChats.RemoveRange(0, RecordedLiveChats.Count - 100);
                        if (!CacheManager.Manager.LastCheckYoutubeLiveStatus[Vtuber].IsLive || CacheManager.Manager.LastCheckYoutubeLiveStatus[Vtuber].VideoId != VideoId)
                        {
                            LiveStoppedEvent?.Invoke(LiveChatId, this);
                            break;
                        }
                    }
                    Thread.Sleep(info.PollingInterval + 500);
                }
            }).Start();
        }
    }
}
