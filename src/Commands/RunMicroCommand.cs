using System.Linq;

namespace Microdancer
{
    public sealed class RunMicroCommand : CommandBase
    {
        private readonly LibraryManager _library;
        private readonly MicroManager _microManager;

        public RunMicroCommand(LibraryManager library, MicroManager microManager) : base()
        {
            _library = library;
            _microManager = microManager;
        }

        [Command(
            "runmicro",
            HelpMessage = "Execute a Micro by ID or partial name. Can be supplied with a second region parameter to run only a specific region of a Micro."
        )]
        public void RunMicro(string search, string? region = null)
        {
            RunMicroImpl("runmicro", search, region, false);
        }

        [Command(
            "runparallelmicro",
            "runpmicro",
            "runmultimicro",
            HelpMessage = "Execute a Micro by ID or partial name, in parallel with any other running Micros. Can be supplied with a second region parameter to run only a specific region of a Micro."
        )]
        public void RunMicroParallel(string search, string? region = null)
        {
            RunMicroImpl("runparallelmicro", search, region, true);
        }

        [Command(
            "microcancel",
            HelpMessage = "Cancel the first Micro of a given ID or partial name, or all if no value is passed."
        )]
        public void CancelMicro(string? search = null)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                _microManager.CancelAllMicros();
                return;
            }

            var micro = _library.Find<Micro>(search);
            if (micro == null)
            {
                PrintError("microcancel", $"No Micro with ID or name containing \"{search}\" found.");
                return;
            }

            var entry = _microManager.Running.FirstOrDefault(e => e.Value.Micro.Id == micro.Id);
            if (entry.Value == null)
            {
                PrintError("microcancel", $"That Micro is not running.");
                return;
            }

            _microManager.CancelMicro(entry.Key);
        }

        private void RunMicroImpl(string cmd, string search, string? region, bool multi)
        {
            var micro = _library.Find<Micro>(search);
            if (micro == null)
            {
                PrintError(cmd, $"No Micro with ID or name containing \"{search}\" found.");
                return;
            }

            if (!multi)
            {
                CancelMicro(string.Empty);
            }

            _microManager.SpawnMicro(micro, region);
        }
    }
}