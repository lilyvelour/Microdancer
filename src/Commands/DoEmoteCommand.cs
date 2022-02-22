using System;
using Dalamud.Game.Gui;
using Dalamud.Logging;

namespace Microdancer
{
    public sealed class DoEmoteCommand : CommandBase
    {
        private readonly GameGui _gameGui;
        private readonly GameManager _gameManager;

        public DoEmoteCommand(GameGui gameGui, GameManager gameManager) : base()
        {
            _gameGui = gameGui;
            _gameManager = gameManager;
        }

        [Command("doemote", HelpMessage = "Performs the specified emote by ID.")]
        public void DoEmote(string id)
        {
            _gameManager.emoteAgent =
                (_gameManager.emoteAgent != IntPtr.Zero)
                    ? _gameManager.emoteAgent
                    : _gameGui.FindAgentInterface("Emote");

            if (_gameManager.emoteAgent == IntPtr.Zero)
            {
                PluginLog.LogError(
                    "Failed to get emote agent - open the emote window and then use this command to initialize it."
                );
                return;
            }

            if (uint.TryParse(id, out var emote))
            {
                _gameManager.DoEmote?.Invoke(_gameManager.emoteAgent, emote, 0, true, true);
            }
            else
            {
                PluginLog.LogError("Emote must be specified by an ID.");
            }
        }

        [Command("bed", HelpMessage = "Alias for /doemote 88.")]
        public void Bed()
        {
            DoEmote("88");
        }

        [Command("chair", HelpMessage = "Alias for /doemote 96.")]
        public void Chair()
        {
            DoEmote("96");
        }
    }
}
