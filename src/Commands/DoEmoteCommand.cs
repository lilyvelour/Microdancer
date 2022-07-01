using System;
using System.Threading.Tasks;
using Dalamud.Game.Gui;
using Dalamud.Logging;

namespace Microdancer
{
    public sealed class DoEmoteCommand : CommandBase
    {
        private readonly GameGui _gameGui;
        private readonly GameManager _gameManager;

        public DoEmoteCommand(GameGui gameGui, Service.Locator serviceLocator) : base(serviceLocator)
        {
            _gameGui = gameGui;
            _gameManager = serviceLocator.Get<GameManager>();
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

        [Command("weapon", HelpMessage = "Alias for /doemote 93.")]
        public void Weapon()
        {
            DoEmote("93");
        }

        [Command("idle", "idle1", HelpMessage = "Alias for /setpose stand 1.")]
        public async void Idle1()
        {
            await _gameManager.ExecuteCommand("/setpose stand 1");
        }

        [Command("idle2", HelpMessage = "Alias for /doemote 91")]
        public void Idle2()
        {
            DoEmote("91");
        }

        [Command("idle3", HelpMessage = "Alias for /doemote 92")]
        public void Idle3()
        {
            DoEmote("92");
        }

        [Command("idle4", HelpMessage = "Alias for /doemote 107")]
        public void Idle4()
        {
            DoEmote("107");
        }

        [Command("idle5", HelpMessage = "Alias for /doemote 108")]
        public void Idle5()
        {
            DoEmote("108");
        }

        [Command("idle6", HelpMessage = "Alias for /doemote 218")]
        public void Idle6()
        {
            DoEmote("218");
        }

        [Command("idle7", HelpMessage = "Alias for /doemote 219")]
        public void Idle7()
        {
            DoEmote("219");
        }
    }
}
