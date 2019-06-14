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
using VtuberBot.Spider.Services.Bilibili;
using VtuberBot.Spider.Services.Hiyoko;
using VtuberBot.Spider.Services.Userlocal;

namespace VtuberBot.Bot.Processors
{
    public class VtuberProcessor : BotProcessorBase
    {

        private IMongoCollection<VtuberEntity> _vtuberCollection;


        public VtuberProcessor()
        {
            HelpMessage = "!Vtuber -Vtuber相关操作";
        }

        public override void Process(AcceptedMessage message)
        {
            if (_vtuberCollection == null)
                _vtuberCollection = Observer.Database.GetCollection<VtuberEntity>("vtubers");
            base.Process(message);
        }

        public override async void ShowHelpMessage(AcceptedMessage message, string[] args)
        {
            var str = "使用方法:\r\n" +
                      "!Vtuber add <Vtuber日文名>  -添加Vtuber\r\n" +
                      "!Vtuber list  -查看数据库中的Vtuber列表\r\n" +
                      "!Vtuber 设置中文名 <Vtuber日文名> <中文名> -给Vtuber设置中文名\r\n" +
                      "!Vtuber 设置B站 <Vtuber名字> <B站UID/B站昵称>  -设置Vtuber的B站搬运组\r\n" +
                      "!Vtuber 添加昵称 <Vtuber名字> <昵称>  -给Vtuber添加昵称\r\n" +
                      "!Vtuber 删除昵称 <Vtuber名字> <昵称>  -删除Vtuber的这个昵称";
            await SendingService.SendGroupMessageAsync(message.FromGroup, str);
        }

        public override bool IsMatch(AcceptedMessage message)
        {
            if (message.Content.StartsWith("!") || message.Content.StartsWith("！"))
            {
                var cmd = message.Content.Substring(1, message.Content.Length - 1);
                var keywords = new[] { "vtuber", "vup" };
                return keywords.Any(v => cmd.ToLower().StartsWith(v));
            }
            return false;
        }

        [BotCommand(2)]
        public async void VtuberProfileCommand(AcceptedMessage message, string[] args)
        {
            var vtuber = await GetVtuberAsync(args[1]);
            if (vtuber == null)
            {
                await SendingService.SendGroupMessageAsync(message.FromGroup, "数据库中不存在" + args[1]);
                return;
            }
            await SendingService.SendGroupMessageAsync(message.FromGroup, $"Vtuber详细资料:" +
                                                                          $"\r\n原名: {vtuber.OriginalName}" +
                                                                          $"\r\n中文名: {vtuber.ChineseName}" +
                                                                          $"\r\n昵称: {string.Join(',', vtuber.NickNames)}" +
                                                                          $"\r\nYoutube频道: https://www.youtube.com/channel/{vtuber.YoutubeChannelId}" +
                                                                          $"\r\n推特主页: https://twitter.com/{vtuber.TwitterProfileId}" +
                                                                          $"\r\nB站搬运: https://space.bilibili.com/{vtuber.BilibiliUserId}");
        }

        [BotCommand(1, "list")]
        public async void VtuberListCommand(AcceptedMessage message, string[] args)
        {
            //var vtubers = (await _vtuberCollection.FindAsync(v => true)).ToList();
            //var str = "当前机器人的单推名单:\r\n" + string.Join(",", vtubers.OrderBy(v => v.Group).Select(v => v.OriginalName));
            //await SendingService.SendGroupMessageAsync(message.FromGroup, str);
            await SendingService.SendGroupMessageAsync(message.FromGroup,
                "由于单推太多，发出来会被腾讯当作垃圾信息拦截\r\n请前往https://api.vtb.wiki/webapi/vtuber/list?html=1查看");
        }

        [BotCommand(1, "add")]
        public async void AddVtuberCommand(AcceptedMessage message, string[] args)
        {
            try
            {
                var vtuberName = string.Join(" ", args.Skip(2));
                var vtuber = await GetVtuberAsync(vtuberName);
                if (vtuber != null)
                {
                    await SendingService.SendGroupMessageAsync(message.FromGroup, "已存在 " + vtuberName);
                    return;
                }

                await SendingService.SendGroupMessageAsync(message.FromGroup, "正在搜索, 这可能需要一段时间.");
                var streamers = HiyokoApi.SearchStreamer(vtuberName);
                if (streamers.Count == 0)
                {
                    await SendingService.SendGroupMessageAsync(message.FromGroup, "无法找到: " + vtuberName);
                    return;
                }

                var streamer = streamers.First();
                var bilibiliChannel = streamer.Channels.FirstOrDefault(v => v.ChannelType == 6)?.ChannelId?
                    .Replace("BL_", string.Empty);
                if (await GetVtuberAsync(streamer.Name) != null)
                {
                    await SendingService.SendGroupMessageAsync(message.FromGroup, "已存在 " + vtuberName);
                    return;
                }
                var entity = new VtuberEntity()
                {
                    OriginalName = streamer.Name,
                    TwitterProfileId = streamer.TwitterId,
                    YoutubeChannelId = streamer.Channels.FirstOrDefault(v => v.ChannelType == 1)?.ChannelId,
                    HiyokoProfileId = streamer.Name,
                    BilibiliUserId = string.IsNullOrEmpty(bilibiliChannel) ? 0 : long.Parse(bilibiliChannel)
                };
                var userlocalVtuber = (await UserlocalApi.SearchAsync(vtuberName).Retry(5))
                    .Select(v => UserlocalApi.GetVtuberInfoAsync(v.Id).Retry(5).GetAwaiter().GetResult()).FirstOrDefault(
                        v =>
                        {
                            if (v.ChannelLink.EndsWith("/"))
                                v.ChannelLink = v.ChannelLink.Substring(0, v.ChannelLink.Length - 1);
                            return v.ChannelLink.Split('/').Last() == entity.YoutubeChannelId;
                        });
                if (userlocalVtuber != null)
                {
                    entity.Group = userlocalVtuber.OfficeName;
                    entity.UserlocalProfile = userlocalVtuber.Id;
                }

                await _vtuberCollection.InsertOneAsync(entity);
                await SendingService.SendGroupMessageAsync(message.FromGroup,
                    "已根据互联网相关资料添加: " + vtuberName + "\r\n可使用!Vtuber set修改");
                await SendingService.SendGroupMessageAsync(message.FromGroup, $"Vtuber相关信息: \r\n" +
                                                                              $"原名: {streamer.Name}\r\n" +
                                                                              $"推特主页: https://twitter.com/{streamer.TwitterId}\r\n" +
                                                                              $"Youtube频道: https://www.youtube.com/channel/{streamer.Channels.FirstOrDefault(v => v.ChannelType == 1)?.ChannelId}\r\n" +
                                                                              $"B站主页: https://space.bilibili.com/{entity.BilibiliUserId}\r\n" +
                                                                              $"所属团体: {entity.Group}");
            }
            catch (Exception ex)
            {
                await SendingService.SendGroupMessageAsync(message.FromGroup, "添加vtuber时出现错误, 请重试或反馈, 错误信息: " + ex.Message);
            }

        }

        [BotCommand(4, 1, "设置中文名")]
        public async void SetChineseNameCommand(AcceptedMessage message, string[] args)
        {
            var vtuber = await GetVtuberAsync(args[2]);
            if (vtuber == null)
            {
                await SendingService.SendGroupMessageAsync(message.FromGroup, "未找到Vtuber.");
                return;
            }
            vtuber.ChineseName = args[3];
            await _vtuberCollection.ReplaceOneAsync(v => v.Id == vtuber.Id, vtuber,
                new UpdateOptions() { IsUpsert = true });
            await SendingService.SendGroupMessageAsync(message.FromGroup, "设置完成.");
            try
            {
                var uploader = (await BilibiliApi.SearchBilibiliUsersAsync(vtuber.ChineseName))
                    .OrderByDescending(v => v.Follower).FirstOrDefault(v => v.IsUploader);
                if (uploader != null && vtuber.BilibiliUserId == 0)
                {
                    vtuber.BilibiliUserId = uploader.Id;
                    await _vtuberCollection.ReplaceOneAsync(v => v.Id == vtuber.Id, vtuber,
                        new UpdateOptions() { IsUpsert = true });
                    await SendingService.SendGroupMessageAsync(message.FromGroup, $"已根据中文名自动查找B站搬运组:" +
                                                              $"\r\n用户名: {uploader.Username}" +
                                                              $"\r\n主页: https://space.bilibili.com/{uploader.Id}" +
                                                              $"\r\n粉丝数: {uploader.Follower}");
                    await SendingService.SendGroupMessageAsync(message.FromGroup, "可使用!Vtuber 设置B站 <Vtuber名字> <B站空间ID或搬运组名称> 来修改");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("Search bilibili uploader error.", ex: ex);
            }

        }

        [BotCommand(4, 1, "设置B站")]
        public async void UpdateBilibiliProfileCommand(AcceptedMessage message, string[] args)
        {
            var vtuber = await GetVtuberAsync(args[2]);
            if (vtuber == null)
            {
                await SendingService.SendGroupMessageAsync(message.FromGroup, "未找到相关Vtuber");
                return;
            }
            if (long.TryParse(args[3], out var uid))
            {
                var userInfo = await BilibiliApi.GetBilibiliUserAsync(uid);
                if (userInfo == null)
                {
                    await SendingService.SendGroupMessageAsync(message.FromGroup, "未找到UID: " + uid);
                    return;
                }
                vtuber.BilibiliUserId = uid;
                await _vtuberCollection.ReplaceOneAsync(v => v.Id == vtuber.Id, vtuber,
                    new UpdateOptions() { IsUpsert = true });
                await SendingService.SendGroupMessageAsync(message.FromGroup, $"保存完成:" +
                                                          $"\r\n用户名: {userInfo.Username}" +
                                                          $"\r\n主页: https://space.bilibili.com/{uid}" +
                                                          $"\r\n粉丝数: {userInfo.Follower}");
                return;
            }
            try
            {
                var uploader = (await BilibiliApi.SearchBilibiliUsersAsync(args[3]))
                    .OrderByDescending(v => v.Follower).FirstOrDefault(v => v.IsUploader);
                if (uploader != null)
                {
                    vtuber.BilibiliUserId = uploader.Id;
                    await _vtuberCollection.ReplaceOneAsync(v => v.Id == vtuber.Id, vtuber,
                        new UpdateOptions() { IsUpsert = true });
                    await SendingService.SendGroupMessageAsync(message.FromGroup, $"保存完成:" +
                                                                                  $"\r\n用户名: {uploader.Username}" +
                                                                                  $"\r\n主页: https://space.bilibili.com/{uploader.Id}" +
                                                                                  $"\r\n粉丝数: {uploader.Follower}");
                    return;
                }
                await SendingService.SendGroupMessageAsync(message.FromGroup, "未找到 " + args[3]);
            }
            catch (Exception ex)
            {
                LogHelper.Error("Search bilibili uploader error.", ex: ex);
            }
        }

        [BotCommand(4, 1, "添加昵称")]
        public async void AddNicknameCommand(AcceptedMessage message, string[] args)
        {
            var vtuber = await GetVtuberAsync(args[2]);
            if (vtuber == null)
            {
                await SendingService.SendGroupMessageAsync(message.FromGroup, "未找到Vtuber");
                return;
            }
            if (vtuber.NickNames.Any(v => v.EqualsIgnoreCase(args[3])))
            {
                await SendingService.SendGroupMessageAsync(message.FromGroup, "已存在该昵称");
                return;
            }
            vtuber.NickNames.Add(args[3]);
            await _vtuberCollection.ReplaceOneAsync(v => v.Id == vtuber.Id, vtuber,
                new UpdateOptions() { IsUpsert = true });
            await SendingService.SendGroupMessageAsync(message.FromGroup, "添加完成");
        }

        [BotCommand(4, 1, "删除昵称")]
        public async void RemoveNicknameCommand(AcceptedMessage message, string[] args)
        {
            var vtuber = await GetVtuberAsync(args[2]);
            if (vtuber == null)
            {
                await SendingService.SendGroupMessageAsync(message.FromGroup, "未找到Vtuber");
                return;
            }
            if (vtuber.NickNames.All(v => !v.EqualsIgnoreCase(args[3])))
            {
                await SendingService.SendGroupMessageAsync(message.FromGroup, "已存在该昵称");
                return;
            }
            vtuber.NickNames.RemoveAll(v => v.EqualsIgnoreCase(args[3]));
            await _vtuberCollection.ReplaceOneAsync(v => v.Id == vtuber.Id, vtuber,
                new UpdateOptions() { IsUpsert = true });
            await SendingService.SendGroupMessageAsync(message.FromGroup, "删除完成");
        }

    }
}
