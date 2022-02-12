using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;

namespace Microdancer
{
    public sealed class RunMicroCommand : CommandBase
    {
        private readonly ChatGui _chatGui;
        private readonly LibraryManager _library;
        private readonly MicroManager _microManager;

        public RunMicroCommand(
            CommandManager commandManager,
            Configuration configuration,
            ChatGui chatGui,
            LibraryManager library,
            MicroManager microManager) : base(commandManager, configuration)
        {
            _chatGui = chatGui;
            _library = library;
            _microManager = microManager;
        }

        [Command("runmicro", HelpMessage = "Execute a Micro. Can be supplied with a second region parameter to run only a specific region of a Micro.")]
        public void RunMicro(params string[] args)
        {
            RunMicroImpl(args, false);
        }

        [Command(
            "runparallelmicro",
            "runpmicro",
            "runmultimicro",
            HelpMessage = "Execute a Micro in parallel with any other running Micros. Can be supplied with a second region parameter to run only a specific region of a Micro."
        )]
        public void RunMicroParallel(params string[] args)
        {
            RunMicroImpl(args, true);
        }

        [Command("microcancel", HelpMessage = "Cancel the first Micro of a given type or all if no value is passed.")]
        public void CancelMicro(string search = "")
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                _microManager.CancelAllMicros();
                return;
            }

            var micro = _library.Find<Micro>(search);
            if (micro == null)
            {
                _chatGui.PrintError($"No Micro with ID or name containing \"{search}\" found.");
                return;
            }

            var entry = _microManager.Running.FirstOrDefault(e => e.Value.Micro.Id == micro.Id);
            if (entry.Value == null)
            {
                _chatGui.PrintError($"That Micro is not running.");
                return;
            }

            _microManager.CancelMicro(entry.Key);
        }

        private void RunMicroImpl(string[] args, bool multi)
        {
            if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                _chatGui?.PrintError("Arguments must be either the UUID or partial name of the Micro to execute, along with an optional region.");
                return;
            }

            var search = args[0];

            var micro = _library.Find<Micro>(search);
            if (micro == null)
            {
                _chatGui?.PrintError($"No Micro with ID or name containing \"{search}\" found.");
                return;
            }

            if (!multi)
            {
                CancelMicro(string.Empty);
            }

            var region = args.Length > 1 ? args[1] : null;

            _microManager.SpawnMicro(micro, region);
        }
    }
}