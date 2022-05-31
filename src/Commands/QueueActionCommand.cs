﻿using System;
using System.Threading.Tasks;
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
                PluginLog.LogError($"/q{cmd} is not yet initialized.");
                return;
            }

            // Handle auto translate strings
            var command = $"{cmd} {action.Replace(" ", "\"").Replace(" ", "\"")}";

            await _gameManager.ExecuteCommand(command, !_condition[ConditionFlag.InCombat] ? (byte)0 : (byte)2);
        }
    }
}
