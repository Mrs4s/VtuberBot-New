using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sisters.WudiLib;
using Sisters.WudiLib.Posts;
using Sisters.WudiLib.Responses;
using Sisters.WudiLib.WebSocket;
using VtuberBot.Bot.Attributes;
using VtuberBot.Bot.Models;
using VtuberBot.Core;
using VtuberBot.Core.Extensions;
using Message = Sisters.WudiLib.Message;
using SendingMessage = VtuberBot.Bot.Models.SendingMessage;

namespace VtuberBot.Bot.Services
{
    /// <summary>
    /// Use https://github.com/int-and-his-friends/Sisters.WudiLib lib
    /// </summary>
    [BotService("CoolQ")]
    public class CoolService : IBotService
    {
        public int Level { get; set; }
        public string ListenUrl { get; set; }
        public int ListenPort { get; set; }
        public string WebsocketUrl { get; set; }
        public string AccessToken { get; set; }

        public event Action<IBotService, AcceptedMessage> ReceivedMessageEvent;

        private CqHttpWebSocketEvent _wsApiListener;
        private HttpApiClient _httpClient;

        private readonly Queue<SendingMessage> _sendingMessageQueue = new Queue<SendingMessage>();

        private bool _enabled = false;

        public bool Load()
        {

            if (WebsocketUrl.EndsWith("/"))
                WebsocketUrl = WebsocketUrl.Substring(0, WebsocketUrl.Length - 1);
            _httpClient = new HttpApiClient(ListenUrl, AccessToken);
            _wsApiListener = new CqHttpWebSocketEvent(WebsocketUrl, AccessToken);
            _wsApiListener.StartListen();
            _wsApiListener.MessageEvent += (api, message) =>
            {
                if (message is GroupMessage groupContent)
                {
                    var groupMessage = new AcceptedMessage()
                    {
                        IsGroupMessage = true,
                        FromGroup = groupContent.GroupId,
                        FromUser = groupContent.Sender.UserId,
                        Content = groupContent.Content.Text
                    };
                    ReceivedMessageEvent?.Invoke(this, groupMessage);
                }

                if (message is PrivateMessage privateContent)
                {
                    var privateMessage = new AcceptedMessage()
                    {
                        IsGroupMessage = false,
                        FromUser = privateContent.UserId,
                        Content = privateContent.Content.Text
                    };
                    ReceivedMessageEvent?.Invoke(this, privateMessage);
                }
            };
            _wsApiListener.GroupInviteEvent += (api, request) => true;
            _wsApiListener.GroupAddedEvent += (api, request) =>
            {
                var message = (SendingMessage) "欢迎使用Vtuber-Bot 查看帮助请输入help";
                message.TargetGroupId = request.GroupId;
                _sendingMessageQueue.Enqueue(message);
            };
            _enabled = true;
            new Thread(async () =>
            {
                while (_enabled)
                {
                    await Task.Delay(1500);
                    if (_sendingMessageQueue.TryDequeue(out var message))
                    {
                        var sendingMessage = new Sisters.WudiLib.SendingMessage();
                        foreach (var content in message.Contents)
                        {
                            if (content.Type == MessageType.Image)
                                sendingMessage += Sisters.WudiLib.SendingMessage.ByteArrayImage(content.Data);
                            if (content.Type == MessageType.Text)
                                sendingMessage += new Sisters.WudiLib.SendingMessage(content.Content);
                        }
                        await _httpClient.SendGroupMessageAsync(message.TargetGroupId, sendingMessage)
                            .Retry(5, handle: e => LogHelper.Error("Send message error", ex: e));
                    }
                }
            })
            { IsBackground = true}.Start();
            return true;
        }

        public void Unload()
        {
            _enabled = false;
        }

        public async Task<bool> SendPrivateMessageAsync(long friendId, SendingMessage message)
        {
            var response = await _httpClient.SendPrivateMessageAsync(friendId, message.ToString());
            return true;
        }

        public async Task<bool> SendGroupMessageAsync(long groupId, SendingMessage message)
        {
            if (message.Contents.All(v => v.Type == MessageType.Text))
            {
                var msg = message.ToString();
                if (msg.Contains("https") || msg.Contains("http"))
                    msg = msg.Replace("https://", string.Empty).Replace("http://", string.Empty).Replace(".", "。");
                await _httpClient.SendGroupMessageLimitedAsync(groupId, msg);
                return true;
            }
            message.TargetGroupId = groupId;
            _sendingMessageQueue.Enqueue(message);
            return true;
        }

        public Task<List<FriendInfo>> GetFriendsAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<List<GroupInfo>> GetGroupsAsync()
        {
            using (var client = new HttpClient())
            {
                var json = JObject.Parse(await client.GetStringAsync(_httpClient.ApiAddress +
                                                               "get_group_list?access_token=" +
                                                               _httpClient.AccessToken).Retry(5));
                if (json.Value<int>("retcode") != 0)
                    return new GroupInfo[0].ToList();
                return json["data"].Select(token => new GroupInfo()
                {
                    GroupId = token["group_id"].ToObject<long>(),
                    GroupName = token["group_name"].ToString()
                }).ToList();
            }
        }

        public async Task<List<GroupMember>> GetGroupMembersAsync(long id)
        {
            var list =await _httpClient.GetGroupMemberListAsync(id);
            return list.Select(v => new GroupMember()
            {
                Id = v.UserId,
                CardName = v.DisplayName,
                NickName = v.Nickname,
                IsAdmin = v.Authority == GroupMemberInfo.GroupMemberAuthority.Leader ||
                          v.Authority == GroupMemberInfo.GroupMemberAuthority.Manager
            }).ToList();
        }
    }

    public static class CoolQExtensions
    {
        public static async Task SendGroupMessageLimitedAsync(this HttpApiClient apiClient, long groupId, string message)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    await client.PostAsync(
                        $"{apiClient.ApiAddress}send_group_msg_rate_limited?access_token={apiClient.AccessToken}",
                        new StringContent(JsonConvert.SerializeObject(new
                        {
                            group_id = groupId,
                            message,
                            auto_escape = true
                        }), Encoding.UTF8, "application/json"));
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"Cannot send message to group {groupId}.", true, ex);
                }

            }
        }
    }
}
