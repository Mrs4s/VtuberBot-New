using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VtuberBot.Bot;
using VtuberBot.Core.Entities;

namespace VtuberBot.Controllers
{
    [Route("api/internal/callback")]
    [AllowAnonymous]
    public class SpiderCallbackController : Controller
    {
        private readonly VtuberBotObserver Observer;

        public SpiderCallbackController(VtuberBotObserver observer) => Observer = observer;


        [HttpPost("youtube/video")]
        public IActionResult YoutubeVideoCallback()
        {
            return Ok();
        }

        [HttpPost("youtube/live")]
        public IActionResult YoutubeLiveCallback([FromBody] YoutubeLiveCallbackBody body)
        {
            if (body.Sign != Config.DefaultConfig.CallbackSign)
                return BadRequest();
            Observer.CallYoutubeBeginLive(body);
            return Ok();
        }

        [HttpPost("youtube/live/vtuberCommented")]
        public IActionResult YoutubeLiveCommentedCallback([FromBody] YoutubeLiveChatCallbackBody body)
        {
            if (body.Sign != Config.DefaultConfig.CallbackSign)
                return BadRequest();
            Observer.CallYoutubeComment(body);
            return Ok();
        }

        [HttpPost("youtube/live/stop")]
        public IActionResult YoutubeStopLiveCallback([FromBody] YoutubeLiveCallbackBody body)
        {
            if (body.Sign != Config.DefaultConfig.CallbackSign)
                return BadRequest();
            Observer.CallYoutubeStopLive(body);
            return Ok();
        }

        [HttpPost("youtube/live/uploaded")]
        public IActionResult YoutubeLiveUploadedCallback([FromBody] YoutubeLiveUploadedCallbackBody body)
        {
            if (body.Sign != Config.DefaultConfig.CallbackSign)
                return BadRequest();
            Observer.CallYoutubeLiveUploaded(body);
            return Ok();
        }
       
        [HttpPost("bilibili/live")]
        public IActionResult BilibiliLiveCallback([FromBody] BilibiliLiveCallbackBody body)
        {
            if (body.Sign != Config.DefaultConfig.CallbackSign)
                return BadRequest();
            Observer.CallBilibiliBeginLive(body);
            return Ok();
        }


        [HttpPost("tweet/publish")]
        public IActionResult PublishTweetCallback([FromBody] TwitterCallbackBody body)
        {
            if (body.Sign != Config.DefaultConfig.CallbackSign)
                return BadRequest();
            Observer.CallPublishTweet(body);
            return Ok();
        }

        [HttpPost("tweet/retweeted")]
        public IActionResult RetweetedCallback([FromBody] TwitterCallbackBody body)
        {
            if (body.Sign != Config.DefaultConfig.CallbackSign)
                return BadRequest();
            Observer.CallRetweeted(body);
            return Ok();
        }

        [HttpPost("tweet/reply")]
        public IActionResult ReplyCallback([FromBody] TwitterCallbackBody body)
        {
            if (body.Sign != Config.DefaultConfig.CallbackSign)
                return BadRequest();
            Observer.CallReplyTweet(body);
            return Ok();
        }


    }
}