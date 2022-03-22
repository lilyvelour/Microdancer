using System;
using System.Linq;

namespace Microdancer
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CommandAttribute : Attribute
    {
        public string Command { get; }
        public string[] Aliases { get; }
        public string HelpMessage { get; set; } = string.Empty;
        public bool ShowInHelp { get; set; } = true;
        public bool Raw { get; set; } = false;

        public CommandAttribute(string command, params string[] aliases)
        {
            var cmd = command.ToLowerInvariant();
            if (!cmd.StartsWith("/"))
            {
                cmd = $"/{cmd}";
            }
            Command = cmd;

            Aliases =
                aliases?.Select(alias => alias.StartsWith("/") ? alias : $"/{alias}").OrderBy(a => a).ToArray()
                ?? Array.Empty<string>();
        }
    }
}
