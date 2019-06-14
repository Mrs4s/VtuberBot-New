using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VtuberBot.Bot.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BotCommandAttribute : Attribute
    {
        public int ProcessLength { get; }

        public string SubCommandName { get; }

        public int SubCommandOffset { get; }

        public Permissions Permission { get; }


        public BotCommandAttribute(int processLength, Permissions permission = Permissions.User)
        {
            ProcessLength = processLength;
            Permission = permission;
        }

        public BotCommandAttribute(int processLength, int offset, string subCommandName, Permissions permission = Permissions.User) : this(processLength, permission)
        {
            SubCommandName = subCommandName;
            SubCommandOffset = offset;
        }

        public BotCommandAttribute(int offset, string subCommandName, Permissions permission = Permissions.User)
        {
            SubCommandName = subCommandName;
            SubCommandOffset = offset;
            Permission = permission;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandPermissionsAttribute : Attribute
    {
        public string[] Permissions { get; set; }

        public CommandPermissionsAttribute(params string[] permissions)
        {
            Permissions = permissions;
        }
    }

    public enum Permissions
    {
        Administrator,
        User
    }
}
