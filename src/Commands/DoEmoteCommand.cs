using System;
using Dalamud.Plugin.Services;

namespace Microdancer
{
    public sealed class DoEmoteCommand : CommandBase
    {
        private readonly IGameGui _gameGui;
        private readonly GameManager _gameManager;

        public DoEmoteCommand(IGameGui gameGui, Service.Locator serviceLocator) : base(serviceLocator)
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
                Microdancer.PluginLog.Error(
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
                Microdancer.PluginLog.Error("Emote must be specified by an ID.");
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

        [Command("chair2", HelpMessage = "Alias for /doemote 95.")]
        public void Chair2()
        {
            DoEmote("95");
        }

        [Command("chair3", HelpMessage = "Alias for /doemote 254.")]
        public void Chair3()
        {
            DoEmote("254");
        }

        [Command("chair4", HelpMessage = "Alias for /doemote 255.")]
        public void Chair4()
        {
            DoEmote("255");
        }

        [Command("weapon", HelpMessage = "Alias for /doemote 93.")]
        public void Weapon()
        {
            DoEmote("93");
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

        [Command("parasol2", HelpMessage = "Alias for /doemote 243")]
        public void Parasol2()
        {
            DoEmote("243");
        }

        [Command("parasol3", HelpMessage = "Alias for /doemote 244")]
        public void Parasol3()
        {
            DoEmote("244");
        }

        [Command("parasol4", HelpMessage = "Alias for /doemote 253")]
        public void Parasol4()
        {
            DoEmote("253");
        }
    }
}
