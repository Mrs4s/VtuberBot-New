using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VtuberBot.Core;
using VtuberBot.Core.Extensions;
using Formatting = Newtonsoft.Json.Formatting;
using TaskExtensions = VtuberBot.Core.Extensions.TaskExtensions;

namespace VtuberBot.Spider.Services.Bilibili.Live
{
    public class BilibiliLiveClient
    {
        public long RoomId { get; }

        public string DanmakuServer { get; private set; }

        public int Popularity { get; private set; }

        public int MaxPopularity { get; private set; }

        public ClientWebSocket WebSocket { get; private set; }



        #region Events

        public event Action<BilibiliGiftInfo> GotGiftEvent;

        public event Action<BilibiliDanmakuInfo> GotDanmakuEvent;

        public event Action<BilibiliLiveClient> SocketDisconnectEvent;

        public event Action<BilibiliLiveClient> LiveBeginEvent;

        public event Action<BilibiliLiveClient> LiveStoppedEvent;
        #endregion



        public BilibiliLiveClient(long roomId)
        {
            RoomId = roomId;

        }

        public void InitDanmakuServer()
        {
            using (var client = HttpClientExtensions.CreateClient())
            {
                var xml = client.GetStringAsync("https://live.bilibili.com/api/player?id=cid:" + RoomId).GetAwaiter().GetResult();
                xml = $"<root>{xml}</root>";
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                DanmakuServer = doc["root"]["dm_host_list"].InnerText.Split(',').First();
            }
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                WebSocket = new ClientWebSocket();
                if (string.IsNullOrEmpty(DanmakuServer))
                    InitDanmakuServer();
                await WebSocket.ConnectAsync(new Uri($"wss://{DanmakuServer}/sub"), CancellationToken.None);
                await SendJoinPacketAsync();
                new Thread(() =>
                {
                    while (WebSocket.State == WebSocketState.Open)
                    {
                        try
                        {
                            SendHeartbeatPacketAsync().Retry(5).GetAwaiter().GetResult();
                            Thread.Sleep(1000 * 30);
                        }
                        catch
                        {
                            //TODO: process this
                        }
                    }

                    SocketDisconnectEvent?.Invoke(this);
                }).Start();
                BeginProcessPacket();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Connect to danmaku server {DanmakuServer} error.", true, ex);
                return false;
            }
        }

        public void CloseConnect()
        {
            try
            {
                if (WebSocket.State == WebSocketState.Open)
                {
                    WebSocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None).GetAwaiter()
                        .GetResult();
                }
            }
            catch
            {
                //
            }

        }

        private void BeginProcessPacket()
        {
            new Thread(async () =>
            {
                while (WebSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        var buffer = new byte[40960];  //Fucking kimo风暴
                        var result = await WebSocket.ReceiveAsync(buffer, CancellationToken.None);
                        var bytes = new byte[result.Count];
                        Array.Copy(buffer, bytes, result.Count);
                        using (var memory = new MemoryStream(bytes))
                        using (var reader = new BinaryReader(memory))
                        {
                            while (memory.Position < memory.Length)
                            {
                                var length = reader.BeReadInt32();  //packet length
                                reader.BeReadUInt16();  //header length
                                reader.BeReadUInt16();   //protocol version
                                var operation = (LiveOperationEnum)reader.BeReadInt32();
                                reader.ReadInt32(); // Sequence Id
                                var playLoad = reader.ReadBytes(length - 16);
                                switch (operation)
                                {
                                    case LiveOperationEnum.COMMAND:
                                        ProcessCommand(playLoad);
                                        break;
                                    case LiveOperationEnum.POPULARITY:
                                        Popularity = (playLoad[0] << 24) | (playLoad[1] << 16) | (playLoad[2] << 8) | playLoad[3];
                                        if (Popularity > MaxPopularity)
                                            MaxPopularity = Popularity;
                                        break;
                                    case LiveOperationEnum.RECEIVE_HEART_BEAT:
                                        break;
                                    default:
                                        LogHelper.Error("Unknown operation id " + (int)operation);
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error("Process danmu packet error.", true, ex);
                    }
                }
            }).Start();
        }

        private void ProcessCommand(byte[] playLoad)
        {
            var json = JObject.Parse(Encoding.UTF8.GetString(playLoad));
            var a = json.ToString();
            var cmd = json["cmd"].ToObject<string>();
            if (cmd.StartsWith("SEND_GIFT"))//礼物
            {
                var giftInfo = json["data"].ToObject<BilibiliGiftInfo>();
                GotGiftEvent?.Invoke(giftInfo);
            }
            else if (cmd.StartsWith("DANMU_MSG"))
            {
                var info = new BilibiliDanmakuInfo()
                {
                    Username = json["info"][2][1].ToString(),
                    Userid = json["info"][2][0].ToObject<long>(),
                    Suffix = json["info"][3].Any() ? json["info"][3][1].ToString() : string.Empty,
                    SuffixRoom = json["info"][3].Any() ? json["info"][3][2].ToString() : string.Empty,
                    SuffixLevel = json["info"][3].Any() ? json["info"][3][0].ToObject<int>() : 0,
                    Message = json["info"][1].ToString(),
                    IsAdmin = json["info"][2][2].ToString() == "1",
                    IsVip = json["info"][2][3].ToString() == "1"
                };
                GotDanmakuEvent?.Invoke(info);
            }
            else if (cmd.StartsWith("GUARD_BUY"))
            {
                var userGuardLevel = json["data"]["guard_level"].ToObject<int>();
                var guardInfo = new BilibiliGiftInfo()
                {
                    Userid = json["data"]["uid"].ToObject<long>(),
                    Username = json["data"]["username"].ToString(),
                    GiftName = userGuardLevel == 3 ? "舰长" :
                        userGuardLevel == 2 ? "提督" :
                        userGuardLevel == 1 ? "总督" : string.Empty,
                    CoinType = "gold",
                    CostCoin = userGuardLevel == 3 ? 190000 :
                        userGuardLevel == 2 ? 2000000 :
                        userGuardLevel == 1 ? 20000000 : 0,
                    Count = json["data"]["num"].ToObject<int>()
                };
                GotGiftEvent?.Invoke(guardInfo);
            }
            else if (cmd.StartsWith("LIVE"))
            {
                LiveBeginEvent?.Invoke(this);
            }
            else if (cmd.StartsWith("PREPARING"))
            {
                LiveStoppedEvent?.Invoke(this);
            }
            /*
            switch (cmd)
            {
                case "SEND_GIFT": 
                    var giftInfo = json["data"].ToObject<BilibiliGiftInfo>();
                    GotGiftEvent?.Invoke(giftInfo);
                    break;
                case "DANMU_MSG": //弹幕
                    var info = new BilibiliDanmakuInfo()
                    {
                        Username = json["info"][2][1].ToString(),
                        Userid = json["info"][2][0].ToObject<long>(),
                        Suffix = json["info"][3].Any() ? json["info"][3][1].ToString() : string.Empty,
                        SuffixRoom = json["info"][3].Any() ? json["info"][3][2].ToString() : string.Empty,
                        SuffixLevel = json["info"][3].Any() ? json["info"][3][0].ToObject<int>() : 0,
                        Message = json["info"][1].ToString(),
                        IsAdmin = json["info"][2][2].ToString() == "1",
                        IsVip = json["info"][2][3].ToString() == "1"
                    };
                    GotDanmakuEvent?.Invoke(info);
                    break;
                case "GUARD_BUY": //购买舰长
                    var userGuardLevel = json["data"]["guard_level"].ToObject<int>();
                    var guardInfo = new BilibiliGiftInfo()
                    {
                        Userid = json["data"]["uid"].ToObject<long>(),
                        Username = json["data"]["username"].ToString(),
                        GiftName = userGuardLevel == 3 ? "舰长" :
                            userGuardLevel == 2 ? "提督" :
                            userGuardLevel == 1 ? "总督" : string.Empty,
                        CoinType = "gold",
                        CostCoin = userGuardLevel == 3 ? 190000 :
                            userGuardLevel == 2 ? 2000000 :
                            userGuardLevel == 1 ? 20000000 : 0,
                        Count = json["data"]["num"].ToObject<int>()
                    };
                    GotGiftEvent?.Invoke(guardInfo);
                    break;
                case "LIVE":
                    LiveBeginEvent?.Invoke(this);
                    break;
                case "PREPARING":
                    LiveStoppedEvent?.Invoke(this);
                    break;
                default:
                    //Console.WriteLine(json.ToString(Formatting.Indented));
                    break;
            }
            */
        }

        private async Task SendHeartbeatPacketAsync()
        {
            await WebSocket.SendAsync(Pack(string.Empty, LiveOperationEnum.SEND_HEART_BEAT), WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        private async Task SendJoinPacketAsync()
        {
            var join = new
            {
                clientver = "1.5.14",
                platform = "web",
                protover = 1,
                roomid = RoomId,
                uid = 0
            };
            await WebSocket.SendAsync(Pack(join, LiveOperationEnum.AUTH_JOIN), WebSocketMessageType.Binary, true,
                CancellationToken.None);
        }



        private byte[] Pack(object obj, LiveOperationEnum operation)
        {
            var playLoad = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, Formatting.None));
            using (var memory = new MemoryStream())
            using (var writer = new BinaryWriter(memory))
            {
                writer.BeWrite(playLoad.Length + 16);
                writer.BeUshortWrite(16);
                writer.BeUshortWrite(1);
                writer.BeWrite((int)operation);
                writer.BeWrite(1);
                writer.Write(playLoad);
                return memory.GetBuffer();
            }
        }
    }

    public enum LiveOperationEnum
    {
        SEND_HEART_BEAT = 2,
        POPULARITY = 3,
        COMMAND = 5,
        AUTH_JOIN = 7,
        RECEIVE_HEART_BEAT = 8
    }
}
