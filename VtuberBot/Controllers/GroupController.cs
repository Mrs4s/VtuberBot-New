using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using VtuberBot.Bot;
using VtuberBot.Core;
using VtuberBot.Core.Entities;
using VtuberBot.Core.Extensions;
using VtuberBot.Models;
using HttpClientExtensions = VtuberBot.Core.Extensions.HttpClientExtensions;

namespace VtuberBot.Controllers
{
    [Route("api/bot")]
    public class GroupController : Controller
    {
        private readonly VtuberBotObserver _observer;
        private readonly IMongoCollection<VtuberEntity> _vtuberCollection;
        private readonly IMongoCollection<YoutubeLiveFile> _liveFileCollection;
        private readonly IMongoCollection<YoutubeLiveInfo> _liveInfoCollection;

        public GroupController(VtuberBotObserver observer)
        {
            _observer = observer;
            _vtuberCollection = observer.Database.GetCollection<VtuberEntity>("vtubers");
            _liveFileCollection = observer.Database.GetCollection<YoutubeLiveFile>("youtube-live-files");
            _liveInfoCollection = observer.Database.GetCollection<YoutubeLiveInfo>("youtube-live-details");
        }

        public static Dictionary<long, string> CodeDictionary { get; } = new Dictionary<long, string>();

        [HttpPost("verification")]
        [AllowAnonymous]
        public async Task<IActionResult> Verification([FromBody] BotLoginBody body)
        {
            foreach (var vtuberBot in _observer.Bots)
            {
                var groups = await vtuberBot.GetRequesterService().GetGroupsAsync();
                if (groups.All(v => v.GroupId != body.GroupId))
                    continue;
                var code = StringExtensions.RandomString.Substring(0, 6);
                if (CodeDictionary.ContainsKey(body.GroupId))
                    CodeDictionary.Remove(body.GroupId);
                CodeDictionary.Add(body.GroupId, code);
                await vtuberBot.GetSendingService()
                    .SendGroupMessageAsync(body.GroupId, "正在请求网页认证: " + code);
                return Ok(new
                {
                    message = "Please check verification message."
                });
            }
            return NotFound(new
            {
                message = "Group not found."
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] BotLoginBody body, [FromHeader(Name = "User-Agent")]string userAgent)
        {
            if (!CodeDictionary.ContainsKey(body.GroupId))
                return BadRequest(new {message = "Unknown session."});
            if (CodeDictionary[body.GroupId] != body.VerificationCode)
                return BadRequest(new { message = "Verification code error." });
            CodeDictionary.Remove(body.GroupId);
            var token = BotJwt.NewJwt(userAgent, body.GroupId);
            return Json(new
            {
                message = "SUCCESS",
                accessToken = token
            });
        }



        // ------ Group api ------

        [HttpGet("group/config")]
        [Authorize]
        public IActionResult GetGroupConfig()
        {
            var groupId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var config = Config.DefaultConfig.GroupConfigs.FirstOrDefault(v => v.GroupId == groupId);
            if (config == null)
                return NotFound(new { message = "This group not any config." });
            return Json(new
            {
                message = "SUCCESS",
                groupId,
                prePublish = config.PrePublish,
                vtuberConfigs = config.PublishConfigs
            });
        }

        [HttpPost("group/vtuberConfig")]
        [Authorize]
        public IActionResult UpdateVtuberConfig()
        {
            return Ok();
        }


        // ------ Vtuber api ------

        [HttpGet("vtubers")]
        [Authorize]
        public async Task<IActionResult> GetVtuberList()
        {
            return Json(new
            {
                message = "SUCCESS",
                vtubers = (await _vtuberCollection.FindAsync(v => true)).ToList()
            });
        }

        [HttpPost("vtubers")]
        [Authorize]
        public async Task<IActionResult> UpdateVtuber([FromBody] VtuberEntity entity)
        {
            var groupId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (groupId != 399572347)
                return Unauthorized();
            var vtuber =
                (await _vtuberCollection.FindAsync(v => v.OriginalName == entity.OriginalName)).FirstOrDefault();
            if (vtuber != null)
                entity.Id = vtuber.Id;
            _vtuberCollection.ReplaceOne(v => v.Id == entity.Id, entity, new UpdateOptions() { IsUpsert = true });
            return Ok(new
            {
                message = "SUCCESS"
            });
        }

        [HttpGet("vtubers/live")]
        [Authorize]
        public IActionResult GetLiveVtubers()
        {
            return Ok();
        }


        // ------ Record api ------

        [HttpGet("record")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRecordFile([FromQuery]string file)
        {
            var liveFile = (await _liveFileCollection.FindAsync(v => true)).ToList()
                .FirstOrDefault(v => v.FileHash.ToLower().Trim() == file.ToLower().Trim());
            if (liveFile == null)
                return NotFound("未找到对应文件，可能已被删除。");
            LogHelper.Info("Download file " + file);
            using (var client = HttpClientExtensions.CreateClient())
            {
                var json = JToken.Parse(await client.GetStringAsync(
                    "http://api.usmusic.cn/internal/cloudfile/link?serverId=mrs4s-blog&md5=" + file));
                return Redirect(json["link"].ToString());
            }

        }

        [HttpGet("records")]
        public async Task<IActionResult> GetRecordList()
        {
            var files = (await _liveFileCollection.FindAsync(v => true)).ToList();
            var items = files.Select(v =>
            {
                var live = _liveInfoCollection.FindAsync(it => it.VideoId == v.VideoId).GetAwaiter().GetResult()
                    .FirstOrDefault();
                return new
                {
                    videoTitle = live?.Title,
                    channelId = live?.Channel,
                    videoId = v.VideoId,
                    fileName = v.FileName,
                    downloadLink = "http://api.bot.vtb.wiki/api/bot/record?file=" + v.FileHash
                };
            });
            return Json(new
            {
                message = "SUCCESS",
                files = items
            });
        }

        [HttpGet("data/records")]
        public async Task<IActionResult> GetRecordList([FromQuery] string vtuberName)
        {
            var vtuber = (await _vtuberCollection.FindAsync(v => v.OriginalName == vtuberName)).FirstOrDefault();
            if (vtuber == null)
                return NotFound();
            var liveHistory = (await _liveInfoCollection.FindAsync(v => v.Channel == vtuber.YoutubeChannelId)).ToList();
            var files = (await _liveFileCollection.FindAsync(v => true)).ToList()
                .Where(v => liveHistory.Any(live => live.VideoId == v.VideoId));
            var html = "<table><tr><td>直播时间</td><td>直播标题</td><td>下载链接</td></tr>";
            html += string.Join(string.Empty, files.Select(v =>
            {
                var live = liveHistory.First(l => l.VideoId == v.VideoId);
                return $"<tr><td>{live.BeginTime.AddHours(8):yyyy-MM-dd HH:mm:ss}</td><td>{live.Title}</td><td>http://api.bot.vtb.wiki/api/bot/record?file={v.FileHash}</td></tr>";
            }));
            html += "</table>";
            return Content(html, "text/html", Encoding.UTF8);
        }
    }
}