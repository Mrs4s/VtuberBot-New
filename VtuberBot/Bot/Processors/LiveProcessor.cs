using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using VtuberBot.Bot.Attributes;
using VtuberBot.Bot.Models;
using VtuberBot.Core.Entities;

namespace VtuberBot.Bot.Processors
{
    public class LiveProcessor : BotProcessorBase
    {
        public LiveProcessor()
        {
            HelpMessage = "!直播  -查看当前直播列表和高级设置";
        }

        public override async void ShowHelpMessage(AcceptedMessage message, string[] args)
        {
            var live = "当前直播列表: \r\n" +
                       string.Join("\r\n", Observer.LiveList.Select(v => v.Key + " 在 " + v.Value + " 直播中"));
            await SendingService.SendGroupMessageAsync(message.FromGroup, live);
        }

        public override bool IsMatch(AcceptedMessage message)
        {
            var keywords = new[] { "!直播", "!live", "！live", "！直播" };
            return keywords.Any(v => message.Content.ToLower().StartsWith(v));
        }
    }

    public class SubscribeProcessor : BotProcessorBase
    {
        private IMongoCollection<VtuberEntity> _vtuberCollection;

        public SubscribeProcessor()
        {
            HelpMessage = "!订阅  添加或查看本群订阅";
        }

        public override void Process(AcceptedMessage message)
        {
            if (_vtuberCollection == null)
                _vtuberCollection = Observer.Database.GetCollection<VtuberEntity>("vtubers");
            var args = message.Content.Split(' ');
            if (args.Length >= 3)
            {
                var vtuberName = args[1] == "查看" ? args[2] : args[3];
                var vtuber = (_vtuberCollection.FindAsync(v =>
                    v.OriginalName == vtuberName || v.ChineseName == vtuberName || v.NickNames.Any(n => n == vtuberName)).GetAwaiter().GetResult()).ToList();
                if (!vtuber.Any())
                {
                    SendingService.SendGroupMessageAsync(message.FromGroup, "未知Vtuber");
                    return;
                }
                vtuberName = vtuber.First().OriginalName;
                var config = Config.DefaultConfig.GroupConfigs.FirstOrDefault(v => v.GroupId == message.FromGroup);
                if (config == null)
                {
                    Config.DefaultConfig.GroupConfigs.Add(new GroupConfig()
                    {
                        GroupId = message.FromGroup,
                        PublishConfigs = new List<VtuberPublishConfig>()
                    });
                    Config.SaveToDefaultFile(Config.DefaultConfig);
                    config = Config.DefaultConfig.GroupConfigs.First(v => v.GroupId == message.FromGroup);
                }
                if (config.PublishConfigs.All(v => v.VtuberName != vtuberName))
                {
                    config.PublishConfigs.Add(new VtuberPublishConfig()
                    {
                        VtuberName = vtuberName
                    });
                }

                var vtuberConfig = config.PublishConfigs.First(v => v.VtuberName == vtuberName);
                if (args[1] == "查看")
                {

                    SendingService.SendGroupMessageAsync(message.FromGroup,
                        $"==== 本群{vtuberConfig.VtuberName}订阅状态 ====\r\n" +
                        $"推特发推: {vtuberConfig.PublishTweet}\r\n" +
                        $"推特转推: {vtuberConfig.Retweeted}\r\n" +
                        $"推特回推: {vtuberConfig.ReplyTweet}\r\n" +
                        $"油管上传: {vtuberConfig.YoutubeUploadVideo}\r\n" +
                        $"油管开播: {vtuberConfig.YoutubeBeginLive}\r\n" +
                        $"油管评论: {vtuberConfig.YoutubeComment}\r\n" +
                        $"B站开播: {vtuberConfig.BilibiliBeginLive}\r\n" +
                        $"B站上传: {vtuberConfig.BilibiliUploadVideo}\r\n" +
                        $"B站动态: {vtuberConfig.BilibiliPublishDynamic}").GetAwaiter().GetResult();
                    return;
                }

                if (args[1] == "添加")
                {
                    switch (args[2].ToLower())
                    {
                        case "发推":
                            vtuberConfig.PublishTweet = true;
                            break;
                        case "转推":
                            vtuberConfig.Retweeted = true;
                            break;
                        case "回推":
                            vtuberConfig.ReplyTweet = true;
                            break;
                        case "油管开播":
                            vtuberConfig.YoutubeBeginLive = true;
                            break;
                        case "油管上传":
                            vtuberConfig.YoutubeUploadVideo = true;
                            break;
                        case "油管评论":
                            vtuberConfig.YoutubeComment = true;
                            break;
                        case "b站开播":
                            if (vtuber.First().BilibiliUserId == default(long))
                            {
                                SendingService.SendGroupMessageAsync(message.FromGroup, "该Vtuber未绑定B站搬运，请使用!Vtuber 设置中文名 来绑定").GetAwaiter().GetResult();
                                return;
                            }

                            vtuberConfig.BilibiliBeginLive = true;
                            break;
                        case "b站上传":
                            if (vtuber.First().BilibiliUserId == default(long))
                            {
                                SendingService.SendGroupMessageAsync(message.FromGroup, "该Vtuber未绑定B站搬运，请使用!Vtuber 设置中文名 来绑定");
                                return;
                            }

                            vtuberConfig.BilibiliUploadVideo = true;
                            break;
                        case "b站动态":
                            if (vtuber.First().BilibiliUserId == default(long))
                            {
                                SendingService.SendGroupMessageAsync(message.FromGroup, "该Vtuber未绑定B站搬运，请使用!Vtuber 设置中文名 来绑定");
                                return;
                            }
                            vtuberConfig.BilibiliPublishDynamic = true;
                            break;
                        default:
                            SendingService.SendGroupMessageAsync(message.FromGroup, $"未知订阅");
                            return;
                    }
                    SendingService.SendGroupMessageAsync(message.FromGroup, $"成功订阅");
                    Config.SaveToDefaultFile(Config.DefaultConfig);
                    return;
                }

                if (args[1] == "取消")
                {
                    switch (args[2].ToLower())
                    {
                        case "发推":
                            vtuberConfig.PublishTweet = false;
                            break;
                        case "转推":
                            vtuberConfig.Retweeted = false;
                            break;
                        case "回推":
                            vtuberConfig.ReplyTweet = false;
                            break;
                        case "油管开播":
                            vtuberConfig.YoutubeBeginLive = false;
                            break;
                        case "油管上传":
                            vtuberConfig.YoutubeUploadVideo = false;
                            break;
                        case "油管评论":
                            vtuberConfig.YoutubeComment = false;
                            break;
                        case "b站开播":
                            vtuberConfig.BilibiliBeginLive = false;
                            break;
                        case "b站上传":
                            vtuberConfig.BilibiliUploadVideo = false;
                            break;
                        case "b站动态":
                            vtuberConfig.BilibiliPublishDynamic = false;
                            break;
                        default:
                            SendingService.SendGroupMessageAsync(message.FromGroup, $"未知订阅").GetAwaiter().GetResult();
                            return;
                    }
                    SendingService.SendGroupMessageAsync(message.FromGroup, $"成功取消").GetAwaiter().GetResult();
                    Config.SaveToDefaultFile(Config.DefaultConfig);
                    return;
                }
            }
            base.Process(message);
        }

        public override async void ShowHelpMessage(AcceptedMessage message, string[] args)
        {
            var str = "使用方法： \r\n!订阅 <添加/查看/取消> <发推/转推/回推/油管开播/油管上传/油管评论/B站开播/B站上传/B站动态>  <Vtuber名字> -设置订阅状态" +
                      "\r\n!订阅 预告  -设置或取消距离开播还剩10分钟时的预告";
            await SendingService.SendGroupMessageAsync(message.FromGroup, str);
        }

        public override bool IsMatch(AcceptedMessage message)
        {
            if (message.Content.StartsWith("!") || message.Content.StartsWith("！"))
            {

                var cmd = message.Content.Substring(1, message.Content.Length - 1);
                var keywords = new[] { "订阅" };
                return keywords.Any(v => cmd.ToLower().StartsWith(v));
            }
            return false;
        }

        [BotCommand(2, 1, "查看")]
        public async void ShowSubscribeList(AcceptedMessage message, string[] args)
        {
            var config = Config.DefaultConfig.GroupConfigs.FirstOrDefault(v => v.GroupId == message.FromGroup);
            if (config == null)
            {
                await SendingService.SendGroupMessageAsync(message.FromGroup, "本群未订阅任何事件");
                return;
            }
            var str = "本群订阅列表:\r\n";
            str += string.Join(",",
                config.PublishConfigs
                    .Where(v => v.YoutubeUploadVideo || v.YoutubeBeginLive || v.BilibiliBeginLive ||
                                v.BilibiliPublishDynamic || v.BilibiliUploadVideo || v.PublishTweet || v.ReplyTweet ||
                                v.Retweeted).Select(v => v.VtuberName));
            await SendingService.SendGroupMessageAsync(message.FromGroup, str);
        }

        [BotCommand(2, 1, "预告")]
        public async void PrePublishSetting(AcceptedMessage message, string[] args)
        {
            var config = Config.DefaultConfig.GroupConfigs.FirstOrDefault(v => v.GroupId == message.FromGroup);
            if (config == null)
            {
                Config.DefaultConfig.GroupConfigs.Add(new GroupConfig()
                {
                    GroupId = message.FromGroup,
                    PrePublish = true,
                    PublishConfigs = new List<VtuberPublishConfig>()
                });
                Config.SaveToDefaultFile(Config.DefaultConfig);
                await SendingService.SendGroupMessageAsync(message.FromGroup, "设置完成: 已开启预推送");
                return;
            }

            config.PrePublish = !config.PrePublish;
            Config.SaveToDefaultFile(Config.DefaultConfig);
            await SendingService.SendGroupMessageAsync(message.FromGroup,
                "设置完成: 已" + (config.PrePublish ? "开启" : "关闭") + "预推送");
        }



    }
}
