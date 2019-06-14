using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using VtuberBot.Bot;
using VtuberBot.Core.Entities;
using VtuberBot.Core.Extensions;
using VtuberBot.Spider.Services.Twitter;
using VtuberBot.Spider.Services.Youtube;

namespace VtuberBot.Controllers
{
    [Route("webApi")]
    [AllowAnonymous]
    public class WebController : Controller
    {
        private readonly VtuberBotObserver _observer;
        private readonly IMongoCollection<VtuberEntity> _vtuberCollection;
        private readonly IMongoCollection<YoutubeLiveFile> _liveFileCollection;
        private readonly IMongoCollection<YoutubeLiveInfo> _liveInfoCollection;
        private readonly IMongoCollection<YoutubeLiveChat> _liveChatCollection;
        private readonly IMongoCollection<YoutubeWebLiveChat> _webLiveChatCollection;
        private readonly IMongoCollection<TweetInfo> _tweetsCollection;

        public WebController(VtuberBotObserver observer)
        {
            _observer = observer;
            _vtuberCollection = observer.Database.GetCollection<VtuberEntity>("vtubers");
            _liveFileCollection = observer.Database.GetCollection<YoutubeLiveFile>("youtube-live-files");
            _liveInfoCollection = observer.Database.GetCollection<YoutubeLiveInfo>("youtube-live-details");
            _liveChatCollection = observer.Database.GetCollection<YoutubeLiveChat>("youtube-live-chats");
            _webLiveChatCollection = observer.Database.GetCollection<YoutubeWebLiveChat>("youtube-web-live-chats");
            _tweetsCollection = observer.Database.GetCollection<TweetInfo>("tweet-details");
        }

        // ------ Bot api ------
        /*
        [HttpPost("vtuberBot/add")]
        public async IActionResult AddBot([FromBody] AddBotBody body)
        {
            if (_observer.Bots.Any(v => v.Services.Any(ser => ser.WebsocketUrl.EqualsIgnoreCase(body.WebsocketUrl))) ||
                Config.DefaultConfig.Clients.Any(v => v.ClientId.EqualsIgnoreCase(body.Name)))
            {
                return BadRequest(new
                {
                    error = 1,
                    message = "This bot already exists on database."
                });
            }

            var client = new BotServiceClient()
            {
                ClientId = body.Name,
                Services = new[]
                {
                    new BotServiceConfig()
                    {
                        ServiceType = "CoolQ",
                        ListenUrl = body.AccessUrl,
                        AccessToken = body.AccessToken,
                        WsUrl = body.WebsocketUrl
                    }
                }
            };
            Config.DefaultConfig.Clients

        }
        */



        [HttpGet("vtuber/list")]
        public async Task<IActionResult> GetVtubers([FromQuery] int html = 0)
        {
            if (html == 0)
                return Json(new
                {
                    error = 0,
                    message = "SUCCESS",
                    vtubers = (await _vtuberCollection.FindAsync(v => true)).ToList().Select(v => new
                    {
                        id = v.Id.ToString(),
                        name = v.OriginalName,
                        group = v.Group,
                        twitter = "https://twitter.com/" + v.TwitterProfileId,
                        youtube = "https://www.youtube.com/channel/" + v.YoutubeChannelId
                    })
                });
            var htmlContent = "<table><tr><td>Vtuber原名</td><td>Vtuber所属</td><td>Vtuber昵称</td></tr>";
            htmlContent += string.Join(string.Empty,
                (await _vtuberCollection.FindAsync(v => true)).ToList().Select(v =>
                    $"<tr><td>{v.OriginalName}</td><td>{v.Group}</td><td>{string.Join(',', v.NickNames)}</td></tr>"));
            htmlContent += "</table>";
            return Content(htmlContent, "text/html", Encoding.UTF8);
        }

        [HttpGet("vtuber/{id}/liveHistory")]
        public async Task<IActionResult> GetVtuberLiveHistory([FromRoute] string id)
        {
            var vtuber = (await _vtuberCollection.FindAsync(v => v.Id == ObjectId.Parse(id))).FirstOrDefault();
            if (vtuber == null)
            {
                return NotFound(new
                {
                    error = 1,
                    message = "Vtuber has not found."
                });
            }
            var history = (await _liveInfoCollection.FindAsync(v =>
                v.Channel == vtuber.YoutubeChannelId && v.BeginTime >= new DateTime(2019, 4, 1))).ToList();
            return Json(new
            {
                error = 0,
                message = "SUCCESS",
                history = history.Select(v => new
                {
                    title = v.Title,
                    liveId = v.VideoId,
                    beginTime = v.BeginTime.AddHours(8).ToTimestamp(),
                    endTime = v.EndTime.AddHours(8).ToTimestamp()
                })
            });

        }

        [HttpGet("vtuber/search")]
        public async Task<IActionResult> SearchVtuber([FromQuery] string keyword)
        {
            var list = (await _vtuberCollection.FindAsync(v =>
                v.OriginalName == keyword || v.ChineseName == keyword || v.NickNames.Any(n => n == keyword))).ToList();
            if (!list.Any())
            {
                return NotFound(new
                {
                    error = 1,
                    message = "Vtuber has not found."
                });
            }
            return Json(new
            {
                error = 0,
                message = "SUCCESS",
                result = list.Select(v => new
                {
                    id = v.Id.ToString(),
                    name = v.OriginalName,
                    group = v.Group,
                    twitter = "https://twitter.com/" + v.TwitterProfileId,
                    youtube = "https://www.youtube.com/channel/" + v.YoutubeChannelId
                })
            });
        }

        [HttpGet("events")]
        public IActionResult GetEventLogs()
        {
            var events = _observer.EventList;
            return Json(new
            {
                error = 0,
                message = "SUCCESS",
                eventLog = events.OrderByDescending(v => DateTime.Parse(v.Substring(1, 19)))
            });
        }

        [HttpGet("vtuber/{id}/tweets")]
        public async Task<IActionResult> GetVtuberTweets([FromRoute] string id)
        {
            var vtuber = (await _vtuberCollection.FindAsync(v => v.Id == ObjectId.Parse(id))).FirstOrDefault();
            if (vtuber == null)
            {
                return NotFound(new
                {
                    error = 1,
                    message = "Vtuber has not found."
                });
            }

            return Json(new
            {
                error = 0,
                message = "SUCCESS",
                tweets = (await _tweetsCollection.FindAsync(v => v.User.ScreenName == vtuber.TwitterProfileId)).ToList()
                    .Select(v => new
                    {
                        id = v.Id,
                        content = v.Content,
                        publishTime = v.CreateTime.ToTimestamp(),
                        v.ReplyScreenname,
                        replayedTweet = v.RetweetedTweet?.Content
                    })
            });
        }
        // ----- Live api -----

        [HttpGet("live/{id}/info")]
        public async Task<IActionResult> GetLiveInfo([FromRoute] string id)
        {
            var live = (await _liveInfoCollection.FindAsync(v => v.VideoId == id)).FirstOrDefault();
            if (live == null)
            {
                return NotFound(new
                {
                    error = 1,
                    message = "Live has not found."
                });
            }
            var vtuber = (await _vtuberCollection.FindAsync(v => v.YoutubeChannelId == live.Channel)).First();
            var file = (await _liveFileCollection.FindAsync(v => v.VideoId == live.VideoId)).FirstOrDefault();
            return Json(new
            {
                error = 0,
                message = "SUCCESS",
                live = new
                {
                    title = live.Title,
                    liveId = live.VideoId,
                    author = vtuber.Id.ToString(),
                    beginTime = live.BeginTime.AddHours(8).ToTimestamp(),
                    endTime = live.EndTime.AddHours(8).ToTimestamp(),
                    recorded = file != null,
                    superChatInfo = live.SuperchatInfo,
                    exchangeRate = live.ExchangeRate
                }
            });
        }

        [HttpGet("live/{id}/info/viewers")]
        public async Task<IActionResult> GetLiveViewerInfo([FromRoute] string id)
        {
            var live = (await _liveInfoCollection.FindAsync(v => v.VideoId == id)).FirstOrDefault();
            if (live == null)
            {
                return NotFound(new
                {
                    error = 1,
                    message = "Live has not found."
                });
            }
            var viewersTrend = new List<int>();
            var historyCommentedList = new List<string>();
            var liveHistory = _liveInfoCollection
                .FindAsync(v => v.Channel == live.Channel && v.BeginTime < live.BeginTime).GetAwaiter().GetResult()
                .ToList().OrderByDescending(v => v.BeginTime).Take(5);
            var comments = (await _liveChatCollection.FindAsync(v => v.VideoId == live.VideoId)).ToList();
            foreach (var liveInfo in liveHistory)
            {
                var historyComments = _liveChatCollection.FindAsync(v => v.VideoId == liveInfo.VideoId).GetAwaiter()
                    .GetResult().ToList();
                historyComments.ForEach(v =>
                {
                    if (!historyCommentedList.Contains(v.AuthorChannelId))
                        historyCommentedList.Add(v.AuthorChannelId);
                });
                historyComments.Clear();
                GC.Collect();
            }
            foreach (var comment in comments.Where(v => !v.IsSuperChat))
            {
                if (!viewersTrend.Contains(comment.ViewerCount))
                    viewersTrend.Add(comment.ViewerCount);
            }
            var avgViewer = viewersTrend.Average();
            viewersTrend.RemoveAll(v => v - avgViewer > 1500);
            return Json(new
            {
                error = 0,
                message = "SUCCESS",
                info = new
                {
                    maxViewerCount = viewersTrend.Max(),
                    averageViewerCount = viewersTrend.Average(),
                    firstViewerRate =
                        (int)((float)comments.Count(v => !historyCommentedList.Contains(v.AuthorChannelId)) /
                               comments.Count * 100),
                    viewersTrend
                }
            });
        }

        [HttpGet("live/{id}/info/superchat")]
        public async Task<IActionResult> GetLiveSuperchatInfo([FromRoute] string id)
        {
            var live = (await _liveInfoCollection.FindAsync(v => v.VideoId == id)).FirstOrDefault();
            if (live == null)
            {
                return NotFound(new
                {
                    error = 1,
                    message = "Live has not found."
                });
            }
            var realTimeSuperChats = (await _liveChatCollection.FindAsync(v => v.VideoId == id && v.SuperChatDetailsJson != null)).ToList();
            var replaySuperChats = (await _webLiveChatCollection.FindAsync(v => v.VideoId == live.VideoId && v.Type == YoutubeWebLiveChatType.Superchat)).ToList();
            var sponsors = (await _webLiveChatCollection.FindAsync(v => v.VideoId == live.VideoId && v.Type == YoutubeWebLiveChatType.Sponsor)).ToList();
            var replayInfos = replaySuperChats.Select(v => new
            {
                authorChannel = v.AuthorChannelId,
                message = v.Content,
                publishTime = v.PublishTimestamp,
                costType = v.CostType,
                amount = v.CostAmount
            });
            var realTimeInfos = realTimeSuperChats.Select(v => new
            {
                authorChannel = v.AuthorChannelId,
                message = v.DisplayMessage,
                publishTime = v.PublishTime.AddHours(8).ToTimestamp(),
                costType = v.SuperChatDetails["currency"].ToString(),
                amount = float.Parse(Regex
                    .Match(v.SuperChatDetails["amountDisplayString"].ToString().Replace(",", string.Empty),
                        "\\d+(\\.\\d+){0,1}").Groups.First()
                    .Value)
            });
            var totalSuperChatAmount = Math.Max(realTimeSuperChats.Sum(v =>
                live.ExchangeRate[v.SuperChatDetails["currency"].ToString()] * float.Parse(Regex
                    .Match(v.SuperChatDetails["amountDisplayString"].ToString().Replace(",", string.Empty),
                        "\\d+(\\.\\d+){0,1}").Groups.First()
                    .Value)), replaySuperChats.Sum(v => live.ExchangeRate[v.CostType] * v.CostAmount));
            if (replaySuperChats.Count > realTimeSuperChats.Count)
                return Json(new
                {
                    error = 0,
                    message = "SUCCESS",
                    info = new
                    {
                        superChatCount = Math.Max(realTimeSuperChats.Count, replaySuperChats.Count),
                        sponsorCount = sponsors.Count,
                        exchangeRate = live.ExchangeRate,
                        totalSuperChatAmount,
                        totalSponsorAmount = sponsors.Sum(v => v.CostAmount),
                        superChats = replayInfos,
                        sponsors = sponsors.Select(v => new
                        {
                            authorChannel = v.AuthorChannelId,
                            joinTime = v.PublishTime,
                            cost = v.CostAmount
                        })
                    }
                });
            return Json(new
            {
                error = 0,
                message = "SUCCESS",
                info = new
                {
                    superChatCount = Math.Max(realTimeSuperChats.Count, replaySuperChats.Count),
                    sponsorCount = sponsors.Count,
                    exchangeRate = live.ExchangeRate,
                    totalSuperChatAmount,
                    totalSponsorAmount = sponsors.Sum(v => v.CostAmount),
                    superChats = realTimeInfos,
                    sponsors = sponsors.Select(v => new
                    {
                        authorChannel = v.AuthorChannelId,
                        joinTime = v.PublishTime,
                        cost = v.CostAmount
                    })
                }
            });
        }

        [HttpGet("live/now")]
        public IActionResult GetLiveList()
        {
            return Json(new
            {
                error = 0,
                message = "SUCCESS",
                liveVideos = _observer.LiveList.Select(v => new
                {
                    vtuberName = v.Key,
                    vtuberId = _vtuberCollection.FindAsync(vtuber => vtuber.OriginalName == v.Key).GetAwaiter()
                        .GetResult().First().Id.ToString(),
                    liveLink = v.Value
                })
            });
        }

        [HttpGet("live/{id}/record")]
        public async Task<IActionResult> GetLiveRecord([FromRoute] string id)
        {
            var file = (await _liveFileCollection.FindAsync(v => v.VideoId == id)).FirstOrDefault();
            if (file == null)
            {
                return NotFound(new
                {
                    error = 1,
                    message = "Record file has not found."
                });
            }
            return Json(new
            {
                error = 0,
                message = "SUCCESS",
                downloadLink = "http://api.bot.vtb.wiki/api/bot/record?file=" + file.FileHash
            });
        }

        // ----- Live chat api -----





    }
}