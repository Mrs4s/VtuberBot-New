using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VtuberBot.Bot.Attributes;
using VtuberBot.Bot.Models;
using VtuberBot.Core;
using VtuberBot.Core.Extensions;

namespace VtuberBot.Bot.Services
{

    /// <summary>
    /// Use https://github.com/Chocolatl/qqlight-websocket plugin.
    /// 因存在发不出消息的bug，已停止维护此服务
    /// </summary>
    [BotService("LightQQ")]
    public class LightService : IBotService
    {
        public int Level { get; set; } = 10;
        public string ListenUrl { get; set; }
        public int ListenPort { get; set; }
        public string WebsocketUrl { get; set; }

        public string AccessToken { get; set; }
        //public ServiceType Type => ServiceType.Sending | ServiceType.Receive | ServiceType.ApiRequester ;

        public event Action<IBotService, AcceptedMessage> ReceivedMessageEvent;

        public ClientWebSocket Websocket { get; private set; }


        private bool Running { get; set; }

        private Dictionary<string, Action<JToken>> Callbacks { get; } = new Dictionary<string, Action<JToken>>();
        private Queue<SendingMessage> SendingMessageQueue { get; } = new Queue<SendingMessage>();


        public bool Load()
        {
            LogHelper.Info($"正在使用 {ListenUrl} 启动 Light Service.");
            Websocket = new ClientWebSocket();
            Websocket.ConnectAsync(new Uri(ListenUrl), CancellationToken.None).GetAwaiter().GetResult();
            Running = true;
            BeginProcessMessage();
            return true;
        }

        public void Unload()
        {
            Running = false;
        }

        private void BeginProcessMessage()
        {
            new Thread(async () =>
            {
                while (Running)
                {
                    try
                    {
                        var buffer = new byte[1024 * 1024]; //1mb
                        var result = await Websocket.ReceiveAsync(buffer, CancellationToken.None);
                        var json = JToken.Parse(Encoding.UTF8.GetString(buffer.Take(result.Count).ToArray()));
                        if (json["id"] != null && Callbacks.ContainsKey(json["id"].ToString()))
                        {
                            Callbacks[json["id"].ToString()].Invoke(json);
                            Callbacks.Remove(json["id"].ToString());
                            continue;
                        }

                        if (json["event"] != null)
                        {
                            switch (json["event"].ToString())
                            {
                                case "message":
                                    var type = json["params"]["type"].ToObject<LightMessageType>();
                                    if (type == LightMessageType.Group || type == LightMessageType.Friend)
                                    {
                                        var message = new AcceptedMessage()
                                        {
                                            IsGroupMessage = type == LightMessageType.Group,
                                            Content = json["params"]["content"].ToString(),
                                            FromGroup = type == LightMessageType.Group
                                                ? json["params"]["group"].ToObject<long>()
                                                : 0,
                                            FromUser = json["params"]["qq"].ToObject<long>()
                                        };
                                        ReceivedMessageEvent?.Invoke(this, message);
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (Running && Websocket.State != WebSocketState.Open)
                        {
                            try
                            {
                                Websocket = new ClientWebSocket();
                                await Websocket.ConnectAsync(new Uri(ListenUrl), CancellationToken.None);
                            }
                            catch
                            {
                                LogHelper.Error("Cannot reconnect websocket.");
                            }

                        }
                    }
                }
            }).Start();
            Task.Factory.StartNew( async () =>
            {
                while (Running)
                {
                    await Task.Delay(500);
                    if (SendingMessageQueue.Any())
                    {
                        var message = SendingMessageQueue.Dequeue();
                        if (message.Contents.All(v => v.Type != MessageType.Image))
                        {
                            await SendMessageAsync("sendMessage", new
                            {
                                type = LightMessageType.Group,
                                group = message.TargetGroupId.ToString(),
                                content = message.ToString()
                            });
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        public async Task<bool> SendPrivateMessageAsync(long friendId, SendingMessage message)
        {
            await SendMessageAsync("sendMessage", new
            {
                type = LightMessageType.Friend,
                qq = friendId.ToString(),
                content = message.ToString()
            });
            return true;
        }

        public Task<bool> SendGroupMessageAsync(long groupId, SendingMessage message)
        {
            message.TargetGroupId = groupId;
            SendingMessageQueue.Enqueue(message);
            return Task.FromResult(true);
        }

        public async Task<List<FriendInfo>> GetFriendsAsync()
        {
            var json = await SendMessageAsync("getFriendList", null);
            return json.SelectToken("result.result.0.mems").Select(v => new FriendInfo()
            { Id = v["uin"].ToObject<long>(), NickName = v["name"].ToString() }).ToList();
        }

        public async Task<List<GroupInfo>> GetGroupsAsync()
        {
            var json = await SendMessageAsync("getGroupList", null);
            var joinedGroups = json.SelectToken("result.join");
            var managedGroups = json.SelectToken("result.manage");
            var groups = new List<GroupInfo>();
            groups.AddRange(joinedGroups.Select(v => new GroupInfo()
            {
                GroupId = v["gc"].ToObject<long>(),
                GroupName = v["gn"].ToString(),
                OwnerId = v["owner"].ToObject<long>()
            }));
            groups.AddRange(managedGroups.Select(v => new GroupInfo()
            {
                GroupId = v["gc"].ToObject<long>(),
                GroupName = v["gn"].ToString(),
                OwnerId = v["owner"].ToObject<long>()
            }));
            return groups;
        }

        public Task<List<GroupMember>> GetGroupMembersAsync(long id)
        {
            /*
            var memberJson = await SendMessageAsync("getGroupMemberList", new { group = groupInfo.GroupId.ToString() });
            groupInfo.AdminList = memberJson.SelectToken("result.adm")?.ToObject<List<long>>();
            groupInfo.Members = memberJson.SelectToken("result.members")?.ToObject<Dictionary<string, JToken>>()
                .Select(v =>
                    new GroupMember()
                    {
                        Id = long.Parse(v.Key),
                        CardName = v.Value["cd"]?.ToString(),
                        NickName = v.Value["nk"]?.ToString()
                    }).ToList();
             */
            return null;
        }

        public byte[] PackMessage(string method, string requestId, object requestParams)
        {
            if (requestParams == null)
                requestParams = new object();
            var json = JsonConvert.SerializeObject(new JObject()
            {
                ["method"] = method,
                ["params"] = JToken.FromObject(requestParams),
                ["id"] = requestId
            });
            return Encoding.UTF8.GetBytes(json);
        }

        public async Task SendMessageAsync(string method, object requestParams, Action<JToken> callBack)
        {
            var requestId = StringExtensions.RandomString;
            if (callBack != null)
                Callbacks.Add(requestId, callBack);
            await Websocket.SendAsync(PackMessage(method, requestId, requestParams), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }

        public async Task<JToken> SendMessageAsync(string method, object requestParams)
        {
            var waitEvent = new ManualResetEvent(false);
            JToken result = null;
            await SendMessageAsync(method, requestParams, json =>
            {
                result = json;
                waitEvent.Set();
            });
            waitEvent.WaitOne();
            return result;
        }
    }

    public enum LightMessageType
    {
        Friend = 1,
        Group = 2,
        TalkGroup = 4,
        System = 6
    }
}
