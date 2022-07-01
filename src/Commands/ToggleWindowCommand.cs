using Dalamud.Game.Command;
using Dalamud.Plugin;

namespace Microdancer
{
    public sealed class ToggleWindowCommand : CommandBase
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly Configuration _configuration;

        public ToggleWindowCommand(DalamudPluginInterface pluginInterface, Service.Locator serviceLocator)
            : base(serviceLocator)
        {
            _pluginInterface = pluginInterface;
            _configuration = _pluginInterface.Configuration();
        }

        [Command("microdancer", "micro", "micros", HelpMessage = "Open the Microdancer interface.")]
        public void ToggleWindow()
        {
            _configuration.WindowVisible ^= true;
        }
    }
}
