using System;
using System.Collections.Generic;
using System.Text;

namespace VtuberBot.Core.Entities
{

    // ------ Callbacks ------
    public class CallbackBodyBase
    {
        public string Sign { get; set; }

        public string VtuberName { get; set; }
    }

    public class YoutubeLiveCallbackBody : CallbackBodyBase
    {
        public string LiveLink { get; set; }

        public string LiveTitle { get; set; }

        public long ScheduledStartTime { get; set; }

        public long ActualStartTime { get; set; }

        public int ViewersCount { get; set; }
    }

    public class BilibiliLiveCallbackBody : CallbackBodyBase
    {
        public string LiveLink { get; set; }

        public string LiveTitle { get; set; }

        public long StartTime { get; set; }

    }


    public class YoutubeLiveUploadedCallbackBody : CallbackBodyBase
    {
        public string VideoId { get; set; }

        public string FileHash { get; set; }
    }

    public class YoutubeLiveChatCallbackBody : CallbackBodyBase
    {
        public string LiveAuthorName { get; set; }

        public string Message { get; set; }

        public string LiveLink { get; set; }

        public long PublishTime { get; set; }

    }

    public class YoutubeVideoCallbackBody : CallbackBodyBase
    {
        public string VideoLink { get; set; }

        public string VideoTitle { get; set; }

        public long PublishTime { get; set; }
    }

    public class TwitterCallbackBody : CallbackBodyBase
    {
        
        public string Content { get; set; }

        public long PublishTime { get; set; }

        public bool IsReply { get; set; }

        public string ReplyScreenName { get; set; }

        public string RetweetedUsername { get; set; }
    }



    // ------ Bot ------

    public class BotLoginBody
    {
        public long GroupId { get; set; }

        public string VerificationCode { get; set; }
    }

    public class AddBotBody
    {
        public string Name { get; set; }

        public string WebsocketUrl { get; set; }

        public string AccessToken { get; set; }

        public string AccessUrl { get; set; }

        public string DeletePassword { get; set; }
    }

}
