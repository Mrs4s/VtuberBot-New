using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VtuberBot.Bot.Models
{
    public class AcceptedMessage
    {
        public bool IsGroupMessage { get; set; }

        public long FromUser { get; set; }

        public long FromGroup { get; set; }

        public string FromGroupName { get; set; }

        public string FromGroupCard { get; set; }

        public string Content { get; set; }


    }
}
