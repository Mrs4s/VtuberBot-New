using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VtuberBot.Bot.Models;

namespace VtuberBot.Bot.Processors
{

    public class HelpProcessor : BotProcessorBase
    {
        public override bool IsMatch(AcceptedMessage message)
        {
            var keywords = new[] { "帮助", "使用指南", "help" };
            return keywords.Any(v => message.Content.ToLower().StartsWith(v));
        }

        public override async void ShowHelpMessage(AcceptedMessage message, string[] args)
        {
            var str = "=== Vtuber天狗机器人使用指南 === (指令中的参数如果需要空格请使用%代替空格)\r\n";
            str += string.Join("\r\n",
                Observer.Processors.Where(v => v != this && !string.IsNullOrEmpty(v.HelpMessage))
                    .Select(v => v.HelpMessage));
            str += "\r\n机器人进入维护状态，将不会增加新功能，如有问题请使用!留言 告知";
            if (message.IsGroupMessage)
                await SendingService.SendGroupMessageAsync(message.FromGroup, str);
            else
                await SendingService.SendPrivateMessageAsync(message.FromUser, str);
        }


    }
}
