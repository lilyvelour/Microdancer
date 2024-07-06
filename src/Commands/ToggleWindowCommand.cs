using Dalamud.Game.Command;
using Dalamud.Plugin;

namespace Microdancer
{
    public sealed class ToggleWindowCommand : CommandBase
    {
        private readonly IDalamudPluginInterface _pluginInterface;
        private readonly Configuration _configuration;

        public ToggleWindowCommand(IDalamudPluginInterface pluginInterface, Service.Locator serviceLocator)
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
