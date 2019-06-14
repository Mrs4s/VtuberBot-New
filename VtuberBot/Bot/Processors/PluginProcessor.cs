using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VtuberBot.Bot.Attributes;
using VtuberBot.Bot.Models;
using VtuberBot.Plugin;

namespace VtuberBot.Bot.Processors
{
    public class PluginProcessor : BotProcessorBase
    {
        public override void ShowHelpMessage(AcceptedMessage message, string[] args)
        {

        }

        public override bool IsMatch(AcceptedMessage message)
        {
            return message.Content.StartsWith("!plugin");
        }

        public override void Process(AcceptedMessage message)
        {
            if (message.FromUser != 1844812067)
            {
                SendingService.SendGroupMessageAsync(message.FromGroup, "您没有权限执行这个命令.").GetAwaiter().GetResult();
                return;
            }
            base.Process(message);
        }

        [BotCommand(3, 1, "load")]
        public async void EnablePluginCommand(AcceptedMessage message, string[] args)
        {
            var pluginFolder = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
            var plugin = PluginManager.Manager.LoadPlugin(Path.Combine(pluginFolder, args[2]), Observer);
            await SendingService.SendGroupMessageAsync(message.FromGroup, plugin == null ? "Failed." : "Success.");
        }

        [BotCommand(3, 1, "unload")]
        public async void DestroyPluginCommand(AcceptedMessage message, string[] args)
        {
            PluginManager.Manager.UnloadPlugin(args[2]);
            await SendingService.SendGroupMessageAsync(message.FromGroup, "Success.");
        }

        [BotCommand(3, 1, "reload")]
        public async void ReloadPluginCommand(AcceptedMessage message, string[] args)
        {
            var plugin = PluginManager.Manager.GetPlugin(args[2]);
            if (plugin == null)
            {
                await SendingService.SendGroupMessageAsync(message.FromGroup, "Plugin not found.");
                return;
            }
            PluginManager.Manager.UnloadPlugin(plugin);
            PluginManager.Manager.LoadPlugin(plugin.DllPath, Observer);
            await SendingService.SendGroupMessageAsync(message.FromGroup, "Success.");
        }


    }
}
