using System.Linq;

namespace Microdancer
{
    public sealed class RunMicroCommand : CommandBase
    {
        private readonly LibraryManager _library;
        private readonly MicroManager _microManager;

        public RunMicroCommand(Service.Locator serviceLocator) : base(serviceLocator)
        {
            _library = serviceLocator.Get<LibraryManager>();
            _microManager = serviceLocator.Get<MicroManager>();
        }

        [Command(
            "runmicro",
            HelpMessage = "Execute a Micro by ID or partial name. Can be supplied with a second region parameter to run only a specific region of a Micro."
        )]
        public void RunMicro(string search, string? region = null)
        {
            var micro = _library.Find<Micro>(search);
            if (micro == null)
            {
                PrintError($"No Micro with ID or name containing \"{search}\" found.");
                return;
            }

            _microManager.StartMicro(micro, region);
        }

        [Command("microcancel", HelpMessage = "Cancel the currently running micro. Cannot be used in a Micro.")]
        public void CancelMicro()
        {
            _microManager.Current?.Stop();
        }
    }
}
