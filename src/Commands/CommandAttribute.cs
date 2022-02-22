using System;
using System.Collections.Generic;

namespace Microdancer
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CommandAttribute : Attribute
    {
        public string Command { get; set; }
        public IEnumerable<string>? Aliases { get; set; }
        public string HelpMessage { get; set; } = string.Empty;
        public bool ShowInHelp { get; set; } = true;
        public bool Raw { get; set; } = false;

        public CommandAttribute(string command, params string[] aliases)
        {
            Command = command.ToLowerInvariant();

            if (aliases != null)
            {
                Aliases = aliases;
            }
        }
    }
}
