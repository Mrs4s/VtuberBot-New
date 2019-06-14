using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using VtuberBot.Bot.Attributes;
using VtuberBot.Bot.Models;
using VtuberBot.Core.Extensions;

namespace VtuberBot.Bot
{
    public interface IBotService
    {
        int Level { get; set; }

        string ListenUrl { get; set; }

        int ListenPort { get; set; }

        string WebsocketUrl { get; set; }
        string AccessToken { get; set; }

        //ServiceType Type { get; }

        event Action<IBotService, AcceptedMessage> ReceivedMessageEvent;

        bool Load();

        void Unload();

        Task<bool> SendPrivateMessageAsync(long friendId, SendingMessage message);

        Task<bool> SendGroupMessageAsync(long groupId, SendingMessage message);

        Task<List<FriendInfo>> GetFriendsAsync();

        Task<List<GroupInfo>> GetGroupsAsync();

        Task<List<GroupMember>> GetGroupMembersAsync(long id);
    }

    [Flags]
    public enum ServiceType
    {
        Sending = 3,
        Receive,
        ApiRequester
    }

    public class ServiceDispatcher
    {
        public static IBotService SearchBotService(string type)
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            return (from assType in types
                    let attributes = assType.GetCustomAttributes()
                    where attributes.Any(att => att is BotServiceAttribute)
                    let attr = attributes.First(att => att is BotServiceAttribute) as BotServiceAttribute
                    where attr.ServiceName.EqualsIgnoreCase(type)
                    select Activator.CreateInstance(assType) as IBotService).FirstOrDefault();
        }
    }
}
