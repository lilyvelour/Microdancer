namespace Microdancer
{
    public sealed class MicroOnlyCommands : CommandBase
    {
        public MicroOnlyCommands(Service.Locator serviceLocator) : base(serviceLocator) { }

        [Command(
            "autobusy",
            HelpMessage = "[Micro-only] When specified, busy status will be set to on when the Micro begins, and set to off 5 seconds after the micro ends, unless /autobusy is set again in any Micro."
        )]
        public void AutoBusy()
        {
            PrintError("This command can only be used in a Micro.");
        }

        [Command(
            "autocountdown",
            "autocd",
            HelpMessage = "[Micro-only] When specified, all regions following this line will automatically add countdowns. Can be supplied with the optional arguments [start, pulse]."
        )]
        public void AutoCountdown(string _)
        {
            PrintError("This command can only be used in a Micro.");
        }

        [Command(
            "defaultwait",
            HelpMessage = "[Micro-only] Controls the Micro's default wait time in milliseconds for all subsequent lines. When specified without an argument, reverts to a 10-millisecond timer."
        )]
        public void DefaultWait(int? _ = null)
        {
            PrintError("This command can only be used in a Micro.");
        }

        [Command(
            "loop",
            HelpMessage = "[Micro-only] When specified, immediately loops a Micro from its current starting point."
        )]
        public void Loop()
        {
            PrintError("This command can only be used in a Micro.");
        }

        [Command(
            "padding",
            "pad",
            HelpMessage = "[Micro-only] Adds padding to an emote. Syntax: /pad [# of times to pad] <command>. Commands that are padded will execute repeatedly every 5 milliseconds. The default padding is 80 times (400 milliseconds)."
        )]
        public void Padding(string _)
        {
            PrintError("This command can only be used in a Micro.");
        }
    }
}
