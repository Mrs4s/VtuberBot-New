using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VtuberBot.Core;
using VtuberBot.Core.Entities;
using VtuberBot.Spider.Services.Youtube;

namespace VtuberBot.ReplaySpider.Controllers
{
    [Route("api/internal/callback")]
    [AllowAnonymous]
    public class SpiderCallbackController : Controller
    {
        [HttpPost("youtube/live/stop")]
        public IActionResult YoutubeStopLiveCallback([FromBody] YoutubeLiveCallbackBody body)
        {
            new Thread(() =>
                {
                    var videoId = body.LiveLink.Split('=').Last();
                    LogHelper.Info($"将在两个小时后开始爬取 {body.LiveTitle} ({videoId}) 的Live chat replay.");
                    Thread.Sleep(1000 * 60 * 60 * 2);
                    LogHelper.Info($"开始爬取 {body.LiveTitle} ({videoId}) 的Live chat replay.");
                    try
                    {
                        var replay = YoutubeApi.GetWebLiveChatReplayAsync(videoId).GetAwaiter().GetResult();
                        if (replay == null)
                        {
                            LogHelper.Info("获取失败.");
                            return;
                        }
                        Program.Database.GetCollection<YoutubeWebLiveChat>("youtube-web-live-chats").InsertMany(replay);
                        LogHelper.Info($"爬取完成，已保存 {replay.Count()} 条live chat.");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error("出现异常", ex: ex);
                    }
                })
            { IsBackground = true }.Start();
            return Ok();
        }
    }
}