using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VtuberBot.Bot.Models;
using VtuberBot.Core;

namespace VtuberBot.Bot
{
    public class VtuberBot
    {
        public List<IBotService> Services { get; } = new List<IBotService>();

        public string BotName { get; set; }

        public event Action<VtuberBot, IBotService, AcceptedMessage> ReceivedGroupMessageEvent;
        public event Action<VtuberBot, IBotService, AcceptedMessage> ReceivedPrivateMessageEvent;

        private List<GroupInfo> _groupCache;
        private readonly Dictionary<long, List<GroupMember>> _groupMembersCache =new Dictionary<long, List<GroupMember>>();

        public void Init()
        {
            var service = Services.OrderByDescending(v => v.Level).First();
            service.ReceivedMessageEvent += ReceivedMessageEvent;
        }

        private void ReceivedMessageEvent(IBotService sender, AcceptedMessage message)
        {
            var sendingService = Services.OrderByDescending(v => v.Level).First();
            if (message.IsGroupMessage)
            {
                if (_groupCache == null || _groupCache.All(v => v.GroupId != message.FromGroup))
                    _groupCache = GetRequesterService().GetGroupsAsync().GetAwaiter().GetResult();
                if (!_groupMembersCache.ContainsKey(message.FromGroup))
                    _groupMembersCache.Add(message.FromGroup,
                        GetRequesterService().GetGroupMembersAsync(message.FromGroup).GetAwaiter().GetResult());
                message.FromGroupName = _groupCache?.FirstOrDefault(v => v.GroupId == message.FromGroup)?.GroupName;
                var memberInfo = _groupMembersCache[message.FromGroup].FirstOrDefault(v => v.Id == message.FromUser);
                message.FromGroupCard = string.IsNullOrEmpty(memberInfo?.CardName)
                    ? memberInfo?.NickName
                    : memberInfo.CardName;
                LogHelper.Info($"[{BotName}] - 收到来自群 {message.FromGroupName}({message.FromGroup}) 内用户 {message.FromGroupCard}({message.FromUser}) 的消息: {message.Content}");
                ReceivedGroupMessageEvent?.Invoke(this, sendingService, message);
                return;
            }
            LogHelper.Info($"[{BotName}] - 收到来自好友 {message.FromUser} 的消息: {message.Content}");
            ReceivedPrivateMessageEvent?.Invoke(this, sendingService, message);
        }

        public async Task<List<GroupInfo>> GetGroupsAsync(bool cache = true)
        {
            if (!cache || _groupCache == null)
            {
                var apiService = Services.OrderByDescending(v => v.Level).First();
                return await apiService.GetGroupsAsync();
            }
            return _groupCache;
        }

        public IBotService GetSendingService() => Services.OrderByDescending(v => v.Level).First();

        public IBotService GetRequesterService() => Services.OrderByDescending(v => v.Level).First();
    }
}
