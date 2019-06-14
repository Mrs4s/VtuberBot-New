using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VtuberBot.Bot.Attributes;
using VtuberBot.Bot.Models;
using VtuberBot.Core;
using VtuberBot.Core.Extensions;

namespace VtuberBot.Bot.Services
{
    /// <summary>
    /// Use https://github.com/newbe36524/Newbe.Mahua.Framework Framework
    /// 为啥这个沙雕框架不直接从HTTP BODY返回消息
    /// 和Clever很像，但是不能通用，草
    /// 明明是跨平台框架，部分重要api做成一样啊
    /// 已放弃开发，太烂了
    /// </summary>
    [BotService("MPQ")]
    public class MPQService : IBotService
    {
        public int Level { get; set; }
        public string ListenUrl { get; set; }
        public int ListenPort { get; set; }
        public string WebsocketUrl { get; set; }
        public string AccessToken { get; set; }
        public event Action<IBotService, AcceptedMessage> ReceivedMessageEvent;

        public ClientWebSocket Websocket { get; private set; }
        private bool Running { get; set; }
        private long QQ { get; set; }
        private Dictionary<string, Action<JToken>> Callbacks { get; } = new Dictionary<string, Action<JToken>>();

        public bool Load()
        {
            LogHelper.Info($"正在使用 {ListenUrl} 启动 MPQ Service.");
            Websocket = new ClientWebSocket();
            Websocket.ConnectAsync(new Uri(WebsocketUrl), CancellationToken.None).GetAwaiter().GetResult();
            Running = true;
            BeginProcessMessage();
            if (!ListenUrl.EndsWith("/"))
                ListenUrl += "/";
            using (var client = HttpClientExtensions.CreateClient())
                client.PostJsonAsync(ListenUrl + "Api_GetQQList", new object()).GetAwaiter().GetResult();
            return true;
        }


        public void Unload()
        {

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
                        var bytes = new byte[result.Count];
                        Array.Copy(buffer, bytes, result.Count);
                        var json = JToken.Parse(Encoding.UTF8.GetString(bytes));
                        var typeCode = json["TypeCode"].ToString();
                        if (typeCode.EqualsIgnoreCase("Event"))
                        {
                            var type = (MahuaMessageType)json["EventType"].ToObject<int>();
                            if (type == MahuaMessageType.Friend || type == MahuaMessageType.Group)
                            {
                                var message = new AcceptedMessage()
                                {
                                    FromGroup = json["FromNum"].ToObject<long>(),
                                    FromUser = json["EventOperator"].ToObject<long>(),
                                    Content = json["Message"].ToString(),
                                    IsGroupMessage = type == MahuaMessageType.Group
                                };
                                ReceivedMessageEvent?.Invoke(this, message);
                            }
                            continue;
                        }

                        if (typeCode.EqualsIgnoreCase("Api_GetQQListApiOut"))
                        {
                            QQ = json["Result"].ToObject<long>();
                            continue;
                        }
                        //WDNM为什么HTTP API请求过去的返回值会跑到Websocket返回
                        if (Callbacks.ContainsKey(typeCode.ToLower()))
                        {
                            Callbacks[typeCode.ToLower()]?.Invoke(json);
                            Callbacks.Remove(typeCode.ToLower());
                            continue;
                        }
                    }
                    catch
                    {
                        if (Running && Websocket.State != WebSocketState.Open)
                        {
                            try
                            {
                                Websocket = new ClientWebSocket();
                                await Websocket.ConnectAsync(new Uri(WebsocketUrl), CancellationToken.None);
                            }
                            catch
                            {
                                LogHelper.Error("Cannot reconnect websocket.");
                            }

                        }
                    }
                }

            }).Start();
        }

        public Task<bool> SendPrivateMessageAsync(long friendId, SendingMessage message)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SendGroupMessageAsync(long groupId, SendingMessage message)
        {
            using (var client = HttpClientExtensions.CreateClient())
            {
                //NMD为啥MPQ变成了 ‘响应的QQ’了，太不上心了吧
                await client.PostJsonAsync(ListenUrl + "Api_SendMsg", new
                {
                    响应的QQ = QQ.ToString(),
                    信息类型 = MahuaMessageType.Group,
                    参考子类型=0,
                    收信对象群_讨论组 = groupId.ToString(),
                    收信对象 = groupId.ToString(),
                    内容 = message.ToString(),
                });
                return true;
            }
        }

        public Task<List<FriendInfo>> GetFriendsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<GroupInfo>> GetGroupsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<GroupMember>> GetGroupMembersAsync(long id)
        {
            throw new NotImplementedException();
        }
    }
}
