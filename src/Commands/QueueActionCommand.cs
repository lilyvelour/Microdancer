using System;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Logging;

namespace Microdancer
{
    public sealed class QueueActionCommand : CommandBase
    {
        private readonly Condition _condition;
        private readonly GameManager _gameManager;

        public QueueActionCommand(Condition condition, GameManager gameManager) : base()
        {
            _condition = condition;
            _gameManager = gameManager;
        }

        [Command(
            "qaction",
            "qac",
            HelpMessage = "Uses /action but it can queue. Does not queue in combat.",
            Raw = true
        )]
        public void QueueAction(string action)
        {
            QueueActionImpl("/ac", action);
        }

        [Command(
            "qblueaction",
            HelpMessage = "Uses /blueaction but it can queue. Does not queue in combat.",
            Raw = true
        )]
        public void QueueBlueAction(string action)
        {
            // TODO: Does this actually work?
            QueueActionImpl("/blueaction", action);
        }

        private void QueueActionImpl(string cmd, string action)
        {
            if (_gameManager.actionCommandRequestTypePtr == IntPtr.Zero)
            {
                PluginLog.LogError($"/q{cmd} is not yet initialized.");
                return;
            }

            // To prevent queueing actions in combat
            var inCombat = _condition[ConditionFlag.InCombat];

            if (!inCombat)
            {
                _gameManager.ActionCommandRequestType = 0;
            }

            // Handle auto translate strings
            _gameManager.ExecuteCommand($"{cmd} {action.Replace(" ", "\"").Replace(" ", "\"")}");

            if (!inCombat)
            {
                _gameManager.ActionCommandRequestType = 2;
            }
        }
    }
}