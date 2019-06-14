using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VtuberBot.Bot.Attributes
{
    public class BotServiceAttribute : Attribute
    {
        public string ServiceName { get; set; }

        public BotServiceAttribute(string serviceName)
        {
            ServiceName = serviceName;
        }
    }
}
