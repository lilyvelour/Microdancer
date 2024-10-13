using System;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

namespace Microdancer
{
    public sealed class QueueActionCommand : CommandBase
    {
        private readonly GameManager _gameManager;

        public QueueActionCommand(Service.Locator serviceLocator) : base(serviceLocator)
        {
            _gameManager = serviceLocator.Get<GameManager>();
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
            await QueueActionImpl("/blueaction", action);
        }

        private async Task QueueActionImpl(string cmd, string action)
        {
            if (_gameManager.actionCommandRequestTypePtr == IntPtr.Zero)
            {
                Microdancer.PluginLog.Error($"/q{cmd} is not yet initialized.");
                return;
            }

            // Handle auto translate strings
            var command = $"{cmd} {action.Replace(" ", "\"").Replace(" ", "\"")}";

            await _gameManager.ExecuteCommand(command, _gameManager.IsInCombatOrPvP ? (byte)2 : (byte)0);
        }
    }
}
