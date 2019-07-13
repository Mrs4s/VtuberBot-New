using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VtuberBot.Core;
using VtuberBot.Core.Extensions;

namespace VtuberBot.Spider.Services.Youtube
{
    public class YoutubeApi
    {
        private static readonly string ApiKey = Config.DefaultConfig.YoutubeAccessToken;


        public static IWebProxy Proxy { get; set; }




        public static async Task<YoutubeChannel> GetYoutubeChannelAsync(string channelId)
        {
            using (var client = HttpClientExtensions.CreateClient(useGZip: true, referer: "https://www.youtube.com/", proxy: Proxy))
            {
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("zh"));
                var page = await client.GetStringAsync("https://www.youtube.com/channel/" + channelId);
                var livePage = await client.GetStringAsync($"https://www.youtube.com/channel/{channelId}/live");
                var homeJson = ParseYoutubeInitData(page);
                var liveJson = ParseYoutubeInitData(livePage);
                var liveStatus = liveJson
                    .SelectToken(
                        "contents.twoColumnWatchNextResults.results.results.contents.[1].videoSecondaryInfoRenderer.dateText.simpleText")?
                    .ToString();
                var subscriberText = homeJson.SelectToken("header.c4TabbedHeaderRenderer.subscriberCountText.simpleText")?.ToString();
                var microData = homeJson.SelectToken("microformat.microformatDataRenderer");
                var viewerCountText =
                    liveJson.SelectToken(
                            "contents.twoColumnWatchNextResults.results.results.contents.[0].videoPrimaryInfoRenderer.viewCount.videoViewCountRenderer.viewCount.runs.[0].text")
                        ?.ToString();
                var liveVideoId = liveJson.SelectToken(
                        "contents.twoColumnWatchNextResults.results.results.contents.[0].videoPrimaryInfoRenderer.videoActions.menuRenderer.topLevelButtons" +
                        ".[1].toggleButtonRenderer.defaultNavigationEndpoint.modalEndpoint.modal.modalWithTitleAndButtonRenderer.button.buttonRenderer" +
                        ".navigationEndpoint.signInEndpoint.nextEndpoint.watchEndpoint.videoId")?
                    .ToString();
                return new YoutubeChannel()
                {
                    ChannelId = channelId,
                    ChannelName = microData["title"].ToString(),
                    Description = microData["description"].ToString(),
                    Tags = microData["tags"]?.ToObject<string[]>(),
                    SubscriberCount = subscriberText == null ? 0 : int.Parse(Regex
                        .Match(subscriberText.Replace(",", string.Empty), "\\d+(\\.\\d+){0,1}").Groups.First()
                        .Value),
                    NowLive = liveStatus?.Contains("直播开始") ?? false,
                    LiveVideoId = liveVideoId,
                    LiveViewerCount = viewerCountText == null ? 0 : int.Parse(Regex
                        .Match(viewerCountText.Replace(",", string.Empty), "\\d+(\\.\\d+){0,1}").Groups.First()
                        .Value),
                    Face = homeJson.SelectToken("responseContext.webResponseContextExtensionData.webResponseContextPreloadData.preloadThumbnailUrls.[3]")?.ToString()
                };
            }
        }
        public static async Task<bool> NowLive(string channelId)
        {
            using (var client = HttpClientExtensions.CreateClient(useGZip: true))
            {
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("zh"));
                var html = await client.GetStringAsync($"https://www.youtube.com/channel/{channelId}/live");
                var liveJson = ParseYoutubeInitData(html);
                var liveStatus = liveJson
                    .SelectToken(
                        "contents.twoColumnWatchNextResults.results.results.contents.[1].videoSecondaryInfoRenderer.dateText.simpleText")?
                    .ToString();
                return liveStatus?.Contains("直播开始") ?? false;
            }
        }


        public static async Task<List<YoutubeVideo>> GetPlaylistItemsAsync(string playListId, int count = 5)
        {
            using (var client = HttpClientExtensions.CreateClient(useGZip: true))
            {
                var json = JObject.Parse(await client.GetStringAsync(
                    $"https://www.googleapis.com/youtube/v3/playlistItems?part=snippet%2CcontentDetails&maxResults={count}&playlistId={playListId}&key={ApiKey}"));
                var result = (from token in json["items"]
                              let snippet = token["snippet"]
                              select new YoutubeVideo()
                              {
                                  Title = snippet["title"].ToObject<string>(),
                                  PublishTime = DateTime.Parse(snippet["publishedAt"].ToObject<string>()),
                                  ChannelId = snippet["channelId"].ToObject<string>(),
                                  VideoId = snippet["resourceId"]["videoId"].ToObject<string>(),
                                  VideoLink = "https://www.youtube.com/watch?v=" + snippet["resourceId"]["videoId"].ToObject<string>(),
                                  ChannelTitle = snippet["channelTitle"].ToObject<string>(),
                                  ThumbnailLink = snippet["thumbnails"]["default"]["url"].ToObject<string>(),
                                  Description = snippet["description"].ToObject<string>(),
                                  IsLive = false
                              }).ToList();
                return result;
            }

        }

        public static async Task<YoutubeVideo> GetYoutubeVideoAsync(string videoId)
        {
            using (var client = HttpClientExtensions.CreateClient(useGZip: true, referer: "https://www.youtube.com/"))
            {
                var json = JObject.Parse(await client.GetStringAsync(
                    $"https://www.googleapis.com/youtube/v3/videos?id={videoId}&key={ApiKey}&part=liveStreamingDetails,snippet"));
                if (json["pageInfo"]["totalResults"].ToObject<int>() != 1 || json["items"].First["snippet"]["title"].ToString() == "Deleted video")
                    return null;
                var item = json["items"].First();
                var snippet = item["snippet"];
                var result = new YoutubeVideo()
                {
                    Title = snippet["title"].ToObject<string>(),
                    PublishTime = DateTime.Parse(snippet["publishedAt"].ToObject<string>()),
                    ChannelId = snippet["channelId"].ToObject<string>(),
                    VideoId = item["id"].ToObject<string>(),
                    VideoLink = "https://www.youtube.com/watch?v=" + item["id"].ToObject<string>(),
                    ChannelTitle = snippet["channelTitle"].ToObject<string>(),
                    ThumbnailLink = snippet["thumbnails"]["default"]["url"].ToObject<string>(),
                    Description = snippet["description"].ToObject<string>(),
                    IsLive = snippet["liveBroadcastContent"].ToObject<string>() == "live",
                };
                if (item["liveStreamingDetails"] != null)
                    result.LiveDetails = item["liveStreamingDetails"].ToObject<LiveStreamingDetail>();
                var page = await client.GetStringAsync("https://www.youtube.com/watch?v=" + videoId);
                var initData = ParseYoutubeInitData(page);
                var tempId =
                    initData.SelectToken(
                        "contents.twoColumnWatchNextResults.conversationBar.liveChatRenderer.continuations.[0].reloadContinuationData.continuation")?.ToString();
                result.WebLiveChatId = tempId;
                return result;
            }
        }

        /* 信息实在太少，还是采用API获取
        public static async Task GetWebLiveChats(string webLiveChatId)
        {
            using (var client = HttpClientExtensions.CreateClient(useGZip: true, referer: "https://www.youtube.com"))
            {
                var liveChatPage = await client.GetStringAsync(
                    $"https://www.youtube.com/live_chat?continuation=" + webLiveChatId);
                var initData = ParseYoutubeInitData(liveChatPage);
                var chats = initData.SelectToken("continuationContents.liveChatContinuation.actions");
                var a = chats.ToString();
                return;
            }
        }
        */

        public static async Task<IEnumerable<YoutubeWebLiveChat>> GetWebLiveChatReplayAsync(string videoId, string liveChatId, int offset)
        {
            var result = new List<YoutubeWebLiveChat>();
            using (var client = HttpClientExtensions.CreateClient(useGZip: true, referer: "https://www.youtube.com"))
            {
                var str = await client.GetStringAsync(
                        $"https://www.youtube.com/live_chat_replay/get_live_chat_replay?continuation={liveChatId}&playerOffsetMs={offset}&hidden=false&pbj=1")
                    .Retry(5, 1500);
                if (str == null)
                    return new List<YoutubeWebLiveChat>();
                var json = JToken.Parse(str);
                var liveChats = json.SelectToken("response.continuationContents.liveChatContinuation");
                foreach (var action in liveChats["actions"])
                {
                    var items = action["replayChatItemAction"]["actions"];
                    foreach (var item in items)
                    {
                        if (item["addChatItemAction"]?.HasValues ?? false)
                        {
                            if (item.SelectToken("addChatItemAction.item.liveChatTextMessageRenderer") == null)
                                continue; //System message
                            var messageInfo = item.SelectToken("addChatItemAction.item.liveChatTextMessageRenderer");
                            if (messageInfo["message"]["runs"]?.HasValues ?? false)
                            {
                                result.Add(new YoutubeWebLiveChat()
                                {
                                    Id = messageInfo["id"].ToString(),
                                    Content = ":Emoji:",
                                    Type = YoutubeWebLiveChatType.Chat,
                                    AuthorName = messageInfo["authorName"]["simpleText"].ToString(),
                                    AuthorChannelId = messageInfo["authorExternalChannelId"].ToString(),
                                    PublishTime = DateTimeExtensions.TimestampToDateTime(messageInfo["timestampUsec"].ToObject<long>() / 1000000),
                                    LiveChatId = liveChatId,
                                    VideoId = videoId
                                });
                                continue;
                            }
                            result.Add(new YoutubeWebLiveChat()
                            {
                                Id = messageInfo["id"].ToString(),
                                Content = messageInfo["message"]["simpleText"].ToString(),
                                Type = YoutubeWebLiveChatType.Chat,
                                AuthorName = messageInfo["authorName"]["simpleText"].ToString(),
                                AuthorChannelId = messageInfo["authorExternalChannelId"].ToString(),
                                PublishTime = DateTimeExtensions.TimestampToDateTime(messageInfo["timestampUsec"].ToObject<long>() / 1000000),
                                LiveChatId = liveChatId,
                                VideoId = videoId
                            });
                            continue;
                        }
                        if (item.SelectToken("addLiveChatTickerItemAction.item.liveChatTickerSponsorItemRenderer") != null) //Join sponsor
                        {
                            var sponsorInfo =
                                item.SelectToken(
                                        "addLiveChatTickerItemAction.item.liveChatTickerSponsorItemRenderer.showItemEndpoint.showLiveChatItemEndpoint.renderer.liveChatLegacyPaidMessageRenderer");
                            result.Add(new YoutubeWebLiveChat()
                            {
                                Id = sponsorInfo["id"].ToString(),
                                Content = sponsorInfo["detailText"]["simpleText"].ToString(),
                                Type = YoutubeWebLiveChatType.Sponsor,
                                AuthorName = sponsorInfo["authorName"]["simpleText"].ToString(),
                                AuthorChannelId = sponsorInfo["authorExternalChannelId"].ToString(),
                                PublishTime = DateTimeExtensions.TimestampToDateTime(sponsorInfo["timestampUsec"].ToObject<long>() / 1000000),
                                LiveChatId = liveChatId,
                                VideoId = videoId,
                                CostType = "JPY",
                                CostAmount = 490
                            });

                            continue;
                        }
                        if (item["addLiveChatTickerItemAction"]?.HasValues ?? false) //Super chat
                        {
                            if (item.SelectToken("addLiveChatTickerItemAction.item.liveChatTickerPaidMessageItemRenderer.amount.simpleText") == null)
                                continue;
                            var paidInfo = YoutubeWebLiveChat.ParseWebSuperChat(item
                                .SelectToken(
                                    "addLiveChatTickerItemAction.item.liveChatTickerPaidMessageItemRenderer.amount.simpleText")
                                ?.ToString());
                            var paidMessageInfo = item.SelectToken(
                                "addLiveChatTickerItemAction.item.liveChatTickerPaidMessageItemRenderer.showItemEndpoint.showLiveChatItemEndpoint.renderer.liveChatPaidMessageRenderer");
                            if (result.Any(v => v.Id == paidMessageInfo["id"].ToString()))
                                continue;
                            if (paidMessageInfo["message"]?["runs"]?.HasValues ?? false)
                            {
                                result.Add(new YoutubeWebLiveChat()
                                {
                                    Id = paidMessageInfo["id"].ToString(),
                                    Content = ":Emoji:",
                                    Type = YoutubeWebLiveChatType.Superchat,
                                    AuthorName = paidMessageInfo["authorName"]["simpleText"].ToString(),
                                    AuthorChannelId = paidMessageInfo["authorExternalChannelId"].ToString(),
                                    PublishTime = DateTimeExtensions.TimestampToDateTime(paidMessageInfo["timestampUsec"].ToObject<long>() / 1000000),
                                    LiveChatId = liveChatId,
                                    VideoId = videoId,
                                    CostType = paidInfo.Item1,
                                    CostAmount = paidInfo.Item2
                                });
                                continue;
                            }

                            result.Add(new YoutubeWebLiveChat()
                            {
                                Id = paidMessageInfo["id"].ToString(),
                                Content = paidMessageInfo["message"]?["simpleText"]?.ToString() ?? string.Empty,
                                Type = YoutubeWebLiveChatType.Superchat,
                                AuthorName = paidMessageInfo["authorName"]?["simpleText"]?.ToString(),
                                AuthorChannelId = paidMessageInfo["authorExternalChannelId"]?.ToString(),
                                PublishTime = DateTimeExtensions.TimestampToDateTime(paidMessageInfo["timestampUsec"].ToObject<long>() / 1000000),
                                LiveChatId = liveChatId,
                                VideoId = videoId,
                                CostType = paidInfo.Item1,
                                CostAmount = paidInfo.Item2
                            });

                        }
                    }
                }
                return result.OrderBy(v => v.PublishTime);
            }
        }

        public static async Task<IEnumerable<YoutubeWebLiveChat>> GetWebLiveChatReplayAsync(string videoId)
        {
            var video = await GetYoutubeVideoAsync(videoId);
            if (video == null || string.IsNullOrEmpty(video.WebLiveChatId) || video.LiveDetails == null)
                return null;
            var totalTime =
                ((DateTime)video.LiveDetails.ActualEndTime - (DateTime)video.LiveDetails.ActualStartTime);
            var offset = 0;
            var result = new List<YoutubeWebLiveChat>();
            while (offset < totalTime.TotalMilliseconds)
            {
                var replay =
                    (await GetWebLiveChatReplayAsync(videoId, video.WebLiveChatId, offset)).Where(v =>
                        result.All(comment => comment.Id != v.Id));
                if (!replay.Any())
                {
                    offset += 5000;
                    continue;
                }
                offset += 20000;
                result.AddRange(replay);
            }
            return result;
        }

        public static IEnumerable<IEnumerable<YoutubeWebLiveChat>> GetWebLiveChatReplayEnumerable(string videoId)
        {
            var video = GetYoutubeVideoAsync(videoId).GetAwaiter().GetResult();
            if (video == null || string.IsNullOrEmpty(video.WebLiveChatId) || video.LiveDetails == null)
                yield break;
            var totalTime =
                ((DateTime)video.LiveDetails.ActualEndTime - (DateTime)video.LiveDetails.ActualStartTime);
            var offset = 0;
            var result = new List<YoutubeWebLiveChat>();
            while (offset < totalTime.TotalMilliseconds)
            {
                var replay =
                    GetWebLiveChatReplayAsync(videoId, video.WebLiveChatId, offset).GetAwaiter().GetResult().Where(v =>
                        result.All(comment => comment.Id != v.Id));
                yield return replay;
            }
        }


        public static async Task<YoutubeLiveChatInfo> GetLiveChatInfoAsync(string liveChatId)
        {
            try
            {
                using (var client = HttpClientExtensions.CreateClient(useGZip: true))
                    return JsonConvert.DeserializeObject<YoutubeLiveChatInfo>(await client.GetStringAsync(
                        $"https://www.googleapis.com/youtube/v3/liveChat/messages?liveChatId={liveChatId}&part=id%2Csnippet&key={ApiKey}&maxResults=2000"));
            }
            catch
            {
                return null;
            }


        }

        public static JToken ParseYoutubeInitData(string page)
        {
            var initData = Regex.Match(page, "ytInitialData\"] = (.*?)};").Groups.First().Value
                .Replace("ytInitialData\"] = ", string.Empty);
            initData = initData.Substring(0, initData.Length - 1);
            return JToken.Parse(initData);
        }

        public static JToken ParseYoutubePlayerData(string page)
        {
            var playerData = Regex.Match(page, "yt.setConfig(.*?)};").Groups.First().Value
                .Replace("yt.setConfig", string.Empty);
            playerData = playerData.Substring(0, playerData.Length - 1);
            var token = JToken.Parse(playerData);
            var playerResponse = token["args"]["player_response"].ToString();
            return null;
        }

    }
}
