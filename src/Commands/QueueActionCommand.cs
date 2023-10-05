using System;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

namespace Microdancer
{
    public sealed class QueueActionCommand : CommandBase
    {
        private readonly GameManager _gameManager;
        private readonly IPluginLog _pluginLog;

        public QueueActionCommand(Service.Locator serviceLocator, IPluginLog pluginLog) : base(serviceLocator)
        {
            _gameManager = serviceLocator.Get<GameManager>();
            _pluginLog = pluginLog;
        }

        [Command(
            "qaction",
            "qac",
            HelpMessage = "Uses /action but it can queue. Does not queue in combat.",
            Raw = true
        )]
        public async void QueueAction(string action)
        {
            await QueueActionImpl("/ac", action);
        }

        [Command(
            "qblueaction",
            HelpMessage = "Uses /blueaction but it can queue. Does not queue in combat.",
            Raw = true
        )]
        public async void QueueBlueAction(string action)
        {
            // TODO: Does this actually work?
            await QueueActionImpl("/blueaction", action);
        }

        private async Task QueueActionImpl(string cmd, string action)
        {
            if (_gameManager.actionCommandRequestTypePtr == IntPtr.Zero)
            {
                _pluginLog.Error($"/q{cmd} is not yet initialized.");
                return;
            }

            // Handle auto translate strings
            var command = $"{cmd} {action.Replace(" ", "\"").Replace(" ", "\"")}";

            await _gameManager.ExecuteCommand(command, _gameManager.IsInCombatOrPvP ? (byte)2 : (byte)0);
        }
    }
}
