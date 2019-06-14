using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using VtuberBot.Bot.Attributes;
using VtuberBot.Bot.Models;
using VtuberBot.Core;
using VtuberBot.Core.Entities;
using VtuberBot.Core.Extensions;

namespace VtuberBot.Bot
{
    public class VtuberBotObserver
    {
        public List<VtuberBot> Bots { get; } = new List<VtuberBot>();

        public List<BotProcessorBase> Processors { get; } = new List<BotProcessorBase>();

        public Dictionary<string, string> LiveList { get; } = new Dictionary<string, string>();
        public List<string> EventList { get; } = new List<string>();

        public IMongoDatabase Database { get; }

        private readonly IMongoCollection<YoutubeLiveInfo> _liveInfoCollection;

        public VtuberBotObserver(IMongoDatabase database)
        {
            Database = database;
            _liveInfoCollection = database.GetCollection<YoutubeLiveInfo>("youtube-live-details");
        }

        public void AddBot(VtuberBot bot)
        {
            Bots.Add(bot);
            bot.ReceivedGroupMessageEvent += ReceivedMessageEvent;
            bot.ReceivedPrivateMessageEvent += ReceivedMessageEvent;
        }



        private async void ReceivedMessageEvent(VtuberBot sender, IBotService sendingService, AcceptedMessage message)
        {
            await Task.Run(() =>
            {
                var processor = Processors.FirstOrDefault(v => v.IsMatch(message));
                if (processor == null) return;
                try
                {
                    processor.SendingService = sendingService;
                    processor.Database = Database;
                    processor.Observer = this;
                    processor.Process(message);
                }
                catch (Exception ex)
                {
                    LogHelper.Error("处理消息时出现未知异常 包名：" + processor?.GetType(), true, ex);
                }
            });
        }


        #region Events

        public event Action<YoutubeLiveCallbackBody> VtuberBeginYoutubeLiveEvent;

        public event Action<YoutubeLiveCallbackBody> VtuberStopYoutubeLiveEvent;

        #endregion


        #region Callbacks
        public void CallYoutubeBeginLive(YoutubeLiveCallbackBody body)
        {
            if (!LiveList.ContainsKey(body.VtuberName))
            {
                LogHelper.Info($"Vtuber [{body.VtuberName}] 已开始直播 {body.LiveLink} ({body.LiveTitle}) -{DateTimeExtensions.TimestampToDateTime(body.ActualStartTime).AddHours(8):yyyy-MM-dd HH:mm:ss}");
                InsertEventLog($"[{DateTimeExtensions.TimestampToDateTime(body.ActualStartTime).AddHours(8):yyyy-MM-dd HH:mm:ss}] Vtuber [{body.VtuberName}] 开始直播 {body.LiveTitle} ({body.LiveLink})");
                if (DateTime.Now - DateTimeExtensions.TimestampToDateTime(body.ActualStartTime).AddHours(8) <
                    TimeSpan.FromMinutes(10))
                {
                    foreach (var vtuberBot in Bots)
                    {
                        var groups = vtuberBot.GetGroupsAsync().GetAwaiter().GetResult();
                        if (groups == null)
                            continue;
                        foreach (var groupInfo in groups)
                        {
                            var config = Config.DefaultConfig.GroupConfigs.FirstOrDefault(v => v.GroupId == groupInfo.GroupId)
                                ?.PublishConfigs.FirstOrDefault(v => v.VtuberName == body.VtuberName);
                            if (config == null || !config.YoutubeBeginLive || LiveList.ContainsKey(body.VtuberName))
                                continue;
                            try
                            {

                                vtuberBot.GetSendingService().SendGroupMessageAsync(groupInfo.GroupId,
                                        $"{body.VtuberName} 在 {DateTimeExtensions.TimestampToDateTime(body.ActualStartTime).AddHours(8):yyyy-MM-dd HH:mm:ss} 开始了直播 {body.LiveTitle}\r\n" +
                                        $"链接: {body.LiveLink}\r\n当前观众数量: {body.ViewersCount}\r\n" +
                                        $"原定开播时间: {DateTimeExtensions.TimestampToDateTime(body.ScheduledStartTime).AddHours(8):yyyy-MM-dd HH:mm:ss}\r\n" +
                                        $"实际开播时间: {DateTimeExtensions.TimestampToDateTime(body.ActualStartTime).AddHours(8):yyyy-MM-dd HH:mm:ss}")
                                    .GetAwaiter().GetResult();

                            }
                            catch (Exception ex)
                            {
                                LogHelper.Error("Cannot send message.", true, ex);
                            }

                        }
                    }
                }
                LiveList.Add(body.VtuberName, body.LiveLink);
                VtuberBeginYoutubeLiveEvent?.Invoke(body);
            }
        }

        public void CallYoutubeStopLive(YoutubeLiveCallbackBody body)
        {
            if (LiveList.ContainsKey(body.VtuberName))
            {
                LiveList.Remove(body.VtuberName);
                InsertEventLog($"[{DateTimeExtensions.TimestampToDateTime(body.ActualStartTime).AddHours(8):yyyy-MM-dd HH:mm:ss}] Vtuber [{body.VtuberName}] 停止直播");
                VtuberStopYoutubeLiveEvent?.Invoke(body);
            }
        }

        public void CallYoutubeLiveUploaded(YoutubeLiveUploadedCallbackBody body)
        {
            var liveDetail = _liveInfoCollection.FindAsync(v => v.VideoId == body.VideoId).GetAwaiter().GetResult()
                .FirstOrDefault();
            InsertEventLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Vtuber [{body.VtuberName}] {liveDetail.Title} 的直播录像已上传完成, 下载链接: https://api.vtb.wiki/api/bot/record?file=" + body.FileHash);
            foreach (var vtuberBot in Bots)
            {
                var groups = vtuberBot.GetGroupsAsync().GetAwaiter().GetResult();
                if (groups == null || liveDetail==null)
                    continue;
                foreach (var groupInfo in groups)
                {
                    var config = Config.DefaultConfig.GroupConfigs.FirstOrDefault(v => v.GroupId == groupInfo.GroupId)
                        ?.PublishConfigs.FirstOrDefault(v => v.VtuberName == body.VtuberName);
                    if (config == null || !config.YoutubeBeginLive || LiveList.ContainsKey(body.VtuberName))
                        continue;
                    try
                    {

                        vtuberBot.GetSendingService().SendGroupMessageAsync(groupInfo.GroupId,
                                $"Vtuber {body.VtuberName} {liveDetail.Title} 的直播录像已上传完成, 总时长{(liveDetail.EndTime - liveDetail.BeginTime).TotalMinutes:F1}分钟\r\n下载链接: https://api.vtb.wiki/api/bot/record?file=" +
                                body.FileHash)
                            .GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error("Cannot send message.", true, ex);
                    }

                }
            }
        }

        public void CallYoutubeComment(YoutubeLiveChatCallbackBody body)
        {
            InsertEventLog($"[{DateTimeExtensions.TimestampToDateTime(body.PublishTime).AddHours(8):yyyy-MM-dd HH:mm:ss}] Vtuber [{body.VtuberName}]  在 {body.LiveAuthorName} 的直播间发表了评论 {body.Message}");
            foreach (var vtuberBot in Bots)
            {
                var groups = vtuberBot.GetGroupsAsync().GetAwaiter().GetResult();
                if (groups == null)
                    continue;
                foreach (var groupInfo in groups)
                {
                    var config = Config.DefaultConfig.GroupConfigs.FirstOrDefault(v => v.GroupId == groupInfo.GroupId)
                        ?.PublishConfigs.FirstOrDefault(v => v.VtuberName == body.VtuberName);
                    if (config == null || !config.YoutubeComment || LiveList.ContainsKey(body.VtuberName))
                        continue;
                    try
                    {

                        vtuberBot.GetSendingService().SendGroupMessageAsync(groupInfo.GroupId,
                                $"Vtuber {body.VtuberName} 于 {DateTimeExtensions.TimestampToDateTime(body.PublishTime).AddHours(8):yyyy-MM-dd HH:mm:ss} 在 {body.LiveAuthorName} 的直播间发表了评论\r\n" +
                                $"评论内容: {body.Message}\r\n" +
                                $"直播地址: {body.LiveLink}")
                            .GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error("Cannot send message.", true, ex);
                    }

                }
            }
        }

        public void CallBilibiliBeginLive(BilibiliLiveCallbackBody body)
        {
            foreach (var vtuberBot in Bots)
            {
                var groups = vtuberBot.GetGroupsAsync().GetAwaiter().GetResult();
                if (groups == null)
                    continue;
                foreach (var groupInfo in groups)
                {
                    var config = Config.DefaultConfig.GroupConfigs.FirstOrDefault(v => v.GroupId == groupInfo.GroupId)
                        ?.PublishConfigs.FirstOrDefault(v => v.VtuberName == body.VtuberName);
                    if (config == null || !config.BilibiliBeginLive)
                        continue;
                    vtuberBot.GetSendingService().SendGroupMessageAsync(groupInfo.GroupId,
                            $"{body.VtuberName} 于 {DateTimeExtensions.TimestampToDateTime(body.StartTime):yyyy-MM-dd HH:mm:ss} 在B站开始了直播\r\n{body.LiveTitle}\r\b{body.LiveLink}")
                        .GetAwaiter().GetResult();
                }
            }
        }

        public void CallPublishTweet(TwitterCallbackBody body)
        {
            InsertEventLog($"[{DateTimeExtensions.TimestampToDateTime(body.PublishTime):yyyy-MM-dd HH:mm:ss}] Vtuber [{body.VtuberName}] 发布了推特 {body.Content}");
            foreach (var vtuberBot in Bots)
            {
                var groups = vtuberBot.GetGroupsAsync().GetAwaiter().GetResult();
                if (groups == null)
                    continue;
                foreach (var groupInfo in groups)
                {
                    var config = Config.DefaultConfig.GroupConfigs.FirstOrDefault(v => v.GroupId == groupInfo.GroupId)
                        ?.PublishConfigs.FirstOrDefault(v => v.VtuberName == body.VtuberName);
                    if (config == null || !config.PublishTweet)
                        continue;
                    vtuberBot.GetSendingService().SendGroupMessageAsync(groupInfo.GroupId,
                            $"{body.VtuberName} 在 {DateTimeExtensions.TimestampToDateTime(body.PublishTime):yyyy-MM-dd HH:mm:ss} 发布了推特:\r\n{body.Content}")
                        .GetAwaiter().GetResult();
                }
            }
        }

        public void CallRetweeted(TwitterCallbackBody body)
        {
            InsertEventLog($"[{DateTimeExtensions.TimestampToDateTime(body.PublishTime):yyyy-MM-dd HH:mm:ss}] Vtuber [{body.VtuberName}] 转发了 {body.RetweetedUsername} 的推特: {body.Content}");
            foreach (var vtuberBot in Bots)
            {
                var groups = vtuberBot.GetGroupsAsync().GetAwaiter().GetResult();
                if (groups == null)
                    continue;
                foreach (var groupInfo in groups)
                {
                    var config = Config.DefaultConfig.GroupConfigs.FirstOrDefault(v => v.GroupId == groupInfo.GroupId)
                        ?.PublishConfigs.FirstOrDefault(v => v.VtuberName == body.VtuberName);
                    if (config == null || !config.Retweeted)
                        continue;
                    vtuberBot.GetSendingService().SendGroupMessageAsync(groupInfo.GroupId,
                            $"{body.VtuberName} 在 {DateTimeExtensions.TimestampToDateTime(body.PublishTime):yyyy-MM-dd HH:mm:ss} 转发了 {body.RetweetedUsername} 的推特:\r\n{body.Content}")
                        .GetAwaiter().GetResult();
                }
            }
        }

        public void CallReplyTweet(TwitterCallbackBody body)
        {
            InsertEventLog($"[{DateTimeExtensions.TimestampToDateTime(body.PublishTime):yyyy-MM-dd HH:mm:ss}] Vtuber [{body.VtuberName}] 回复了 {body.ReplyScreenName} 的推特: {body.Content}");
            foreach (var vtuberBot in Bots)
            {
                var groups = vtuberBot.GetGroupsAsync().GetAwaiter().GetResult();
                if (groups == null)
                    continue;
                foreach (var groupInfo in groups)
                {
                    var config = Config.DefaultConfig.GroupConfigs.FirstOrDefault(v => v.GroupId == groupInfo.GroupId)
                        ?.PublishConfigs.FirstOrDefault(v => v.VtuberName == body.VtuberName);
                    if (config == null || !config.ReplyTweet)
                        continue;
                    vtuberBot.GetSendingService().SendGroupMessageAsync(groupInfo.GroupId,
                            $"{body.VtuberName} 在 {DateTimeExtensions.TimestampToDateTime(body.PublishTime):yyyy-MM-dd HH:mm:ss} 回复了 {body.ReplyScreenName} 的推特:\r\n{body.Content}")
                        .GetAwaiter().GetResult();
                }
            }
        }


        private void InsertEventLog(string message)
        {
            if (EventList.Count >= 100)
                EventList.RemoveRange(0, 1);
            EventList.Add(message);
        }

        #endregion

    }

    public abstract class BotProcessorBase
    {
        public IBotService SendingService { get; set; }

        public IMongoDatabase Database { get; set; }

        public VtuberBotObserver Observer { get; set; }

        public string HelpMessage { get; set; }

        public abstract void ShowHelpMessage(AcceptedMessage message, string[] args);

        public abstract bool IsMatch(AcceptedMessage message);

        public async Task<VtuberEntity> GetVtuberAsync(string keyword)
        {
            return (await Database.GetCollection<VtuberEntity>("vtubers").FindAsync(v =>
                    v.OriginalName == keyword || v.ChineseName == keyword || v.NickNames.Any(n => n == keyword)))
                .FirstOrDefault();
        }

        public virtual void Process(AcceptedMessage message)
        {
            if (!message.IsGroupMessage)
            {
                SendingService.SendPrivateMessageAsync(message.FromUser, "暂不支持私聊互动");
                return;
            }
            var methods = GetType().GetMethods().Where(v => v.IsDefined(typeof(BotCommandAttribute), false));
            var atts = methods.Select(v =>
                    v.GetCustomAttributes(false).First(att => att.GetType() == typeof(BotCommandAttribute)))
                .Select(v => (BotCommandAttribute)v);
            var handled = false;
            var args = message.Content.Trim().Split(' ').Select(v => v.Replace("%", " ")).ToArray();
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttributes(false);
                var commandAttr = attr.FirstOrDefault(v => v.GetType() == typeof(BotCommandAttribute));
                var commandPermission = attr.FirstOrDefault(v => v.GetType() == typeof(CommandPermissionsAttribute));
                if (commandAttr != null)
                {
                    var info = (BotCommandAttribute)commandAttr;
                    if (args.Length == info.ProcessLength || info.ProcessLength == 0)
                    {
                        if (!string.IsNullOrEmpty(info.SubCommandName))
                        {
                            if (args.Length <= info.SubCommandOffset)
                                continue;
                            if (!args[info.SubCommandOffset].EqualsIgnoreCase(info.SubCommandName))
                                continue;
                        }
                        else
                        {
                            if (atts.Any(v => !string.IsNullOrEmpty(v.SubCommandName) && args.Length > v.SubCommandOffset && args[v.SubCommandOffset] == v.SubCommandName))
                                continue;
                        }
                        try
                        {
                            handled = true;
                            if (commandPermission != null)
                            {
                                var config =
                                    Config.DefaultConfig.GroupConfigs.FirstOrDefault(
                                        v => v.GroupId == message.FromGroup);
                                var permissionInfo = (CommandPermissionsAttribute) commandPermission;
                                if (config == null || permissionInfo.Permissions.Any(v => config.Permissions.Contains(v)))
                                {
                                    SendingService.SendGroupMessageAsync(message.FromGroup, $"您没有权限执行这个操作.")
                                        .GetAwaiter().GetResult();
                                    return;
                                }
                            }
                            method.Invoke(this, new object[]
                            {
                                message,
                                args
                            });
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Error("处理消息时出现未知异常 包名：" + GetType() + " 函数名: " + method.Name, true, ex);
                            SendingService.SendGroupMessageAsync(message.FromGroup, $"处理请求时出现未知异常,处理函数: {method.Name}");
                        }
                    }
                }
            }
            if (!handled)
                ShowHelpMessage(message, args);
        }
    }
}
