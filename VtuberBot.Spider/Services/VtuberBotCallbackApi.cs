using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VtuberBot.Core.Entities;
using VtuberBot.Core.Extensions;
using VtuberBot.Spider.Services.Bilibili;
using VtuberBot.Spider.Services.Bilibili.Live;
using VtuberBot.Spider.Services.Twitter;
using VtuberBot.Spider.Services.Youtube;

namespace VtuberBot.Spider.Services
{
    public class VtuberBotCallbackApi
    {
        public string Url { get; set; }
        public string Sign { get; set; }
        public VtuberBotCallbackApi(string url, string sign)
        {
            if (!url.EndsWith("/"))
                url += "/";
            Url = url;
            Sign = sign;
        }

        public async Task CallYoutubeBeginLiveAsync(VtuberEntity vtuber, YoutubeVideo live)
        {
            using (var client = HttpClientExtensions.CreateClient())
            {
                var body = new YoutubeLiveCallbackBody()
                {
                    VtuberName = vtuber.OriginalName,
                    LiveTitle = live.Title,
                    LiveLink = live.VideoLink,
                    ScheduledStartTime = live.LiveDetails.ScheduledStartTime.ToTimestamp(),
                    ActualStartTime = live.LiveDetails.ActualStartTime?.ToTimestamp() ?? DateTime.Now.ToTimestamp(),
                    ViewersCount = live.LiveDetails.ViewersCount == null ? 0 : int.Parse(live.LiveDetails.ViewersCount),
                    Sign = Sign
                };
                await client.PostJsonAsync(Url + "youtube/live", body);
            }
        }

        public async Task CallYoutubeStopLiveAsync(VtuberEntity vtuber, YoutubeVideo live)
        {
            using (var client = HttpClientExtensions.CreateClient())
            {
                var body = new YoutubeLiveCallbackBody()
                {
                    VtuberName = vtuber.OriginalName,
                    LiveTitle = live.Title,
                    LiveLink = live.VideoLink,
                    ScheduledStartTime = live.LiveDetails.ScheduledStartTime.ToTimestamp(),
                    ActualStartTime = live.LiveDetails.ActualStartTime?.ToTimestamp() ?? DateTime.Now.ToTimestamp(),
                    ViewersCount = 0,
                    Sign = Sign
                };
                await client.PostJsonAsync(Url + "youtube/live/stop", body);
            }
        }

        public async Task CallYoutubeCommentedAsync(VtuberEntity commentAuthor, VtuberEntity liveAuthor, YoutubeLiveChat comment)
        {
            using (var client = HttpClientExtensions.CreateClient())
            {
                var body = new YoutubeLiveChatCallbackBody()
                {
                    VtuberName = commentAuthor.OriginalName,
                    LiveAuthorName = liveAuthor.OriginalName,
                    LiveLink = "https://www.youtube.com/watch?v=" + comment.VideoId,
                    Message = comment.DisplayMessage,
                    PublishTime = comment.PublishTime.ToTimestamp(),
                    Sign = Sign
                };
                await client.PostJsonAsync(Url + "youtube/live/vtuberCommented", body);
            }
        }

        public async Task CallYoutubeUploadVideoAsync(VtuberEntity vtuber, YoutubeVideo video)
        {
            using (var client = HttpClientExtensions.CreateClient())
            {
                var body = new YoutubeVideoCallbackBody()
                {
                    VtuberName = vtuber.OriginalName,
                    VideoLink = video.VideoLink,
                    VideoTitle = video.Title,
                    PublishTime = video.PublishTime.ToTimestamp(),
                    Sign = Sign
                };
                await client.PostJsonAsync(Url + "youtube/video", body);
            }
        }

        public async Task CallBilibiliBeginLiveAsync(VtuberEntity vtuber, BilibiliLiveRoom room)
        {
            using (var client = HttpClientExtensions.CreateClient())
            {
                var body = new BilibiliLiveCallbackBody()
                {
                    VtuberName = vtuber.OriginalName,
                    LiveLink = "https://live.bilibili.com/" + vtuber.BilibiliLiveRoomId,
                    LiveTitle = room.Title,
                    StartTime = DateTime.Now.ToTimestamp(),
                    Sign = Sign
                };
                await client.PostJsonAsync(Url + "bilibili/live", body);
            }
        }

        public async Task CallPublishTweetAsync(VtuberEntity vtuber, TweetInfo tweet)
        {
            using (var client = HttpClientExtensions.CreateClient())
            {
                var body = new TwitterCallbackBody()
                {
                    VtuberName = vtuber.OriginalName,
                    Content = tweet.Content,
                    IsReply = tweet.IsReply,
                    PublishTime = tweet.CreateTime.ToTimestamp(),
                    ReplyScreenName = tweet.ReplyScreenname,
                    RetweetedUsername = tweet.RetweetedTweet?.User?.Name,
                    Sign = Sign
                };
                await client.PostJsonAsync(Url + "tweet/publish", body);
            }
        }

        public async Task CallRetweetedAsync(VtuberEntity vtuber, TweetInfo tweet)
        {
            using (var client = HttpClientExtensions.CreateClient())
            {
                var body = new TwitterCallbackBody()
                {
                    VtuberName = vtuber.OriginalName,
                    Content = tweet.Content,
                    IsReply = tweet.IsReply,
                    PublishTime = tweet.CreateTime.ToTimestamp(),
                    ReplyScreenName = tweet.ReplyScreenname,
                    RetweetedUsername = tweet.RetweetedTweet?.User?.Name,
                    Sign = Sign
                };
                await client.PostJsonAsync(Url + "tweet/retweeted", body);
            }
        }

        public async Task CallReplyAsync(VtuberEntity vtuber, TweetInfo tweet)
        {
            using (var client = HttpClientExtensions.CreateClient())
            {
                var body = new TwitterCallbackBody()
                {
                    VtuberName = vtuber.OriginalName,
                    Content = tweet.Content,
                    IsReply = tweet.IsReply,
                    PublishTime = tweet.CreateTime.ToTimestamp(),
                    ReplyScreenName = tweet.ReplyScreenname,
                    RetweetedUsername = tweet.RetweetedTweet?.User?.Name,
                    Sign = Sign
                };
                await client.PostJsonAsync(Url + "tweet/reply", body);
            }
        }
    }
}
