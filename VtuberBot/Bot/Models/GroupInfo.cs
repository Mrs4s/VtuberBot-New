using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VtuberBot.Bot.Models
{
    public class GroupInfo
    {
        public string GroupName { get; set; }

        public long GroupId { get; set; }

        public long OwnerId { get; set; }
    }

    public class GroupMember
    {
        public long Id { get; set; }

        public string NickName { get; set; }

        public string CardName { get; set; }

        public bool IsAdmin { get; set; }
    }
}
