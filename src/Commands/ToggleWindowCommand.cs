using Dalamud.Game.Command;
using Dalamud.Plugin;

namespace Microdancer
{
    public sealed class ToggleWindowCommand : CommandBase
    {
        private readonly DalamudPluginInterface _pluginInterface;

        public ToggleWindowCommand(
            CommandManager commandManager,
            Configuration configuration,
            DalamudPluginInterface pluginInterface) : base(commandManager, configuration)
        {
            _pluginInterface = pluginInterface;
        }

        [Command("microdancer", "micro", "micros", HelpMessage = "Open the Microdancer interface.")]
        public void ToggleWindow()
        {
             Configuration.WindowVisible ^= true;
             _pluginInterface.SavePluginConfig(Configuration);
        }

    }
}