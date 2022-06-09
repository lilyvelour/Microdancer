using System;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;

namespace Microdancer
{
    public sealed class SnapChangeCommand : CommandBase
    {
        private readonly GameManager _gameManager;

        public SnapChangeCommand(GameManager gameManager) : base()
        {
            _gameManager = gameManager;
        }

        [Command(
            "snapchange",
            HelpMessage = "Smoothly change to a gear set and/or glamour plate using a snap emote. Requires the guard emote as well.",
            Raw = true
        )]
        public async Task SnapChange(string gearset)
        {
            if (string.IsNullOrWhiteSpace(gearset))
            {
                PrintError($"Gear set cannot be empty");
                return;
            }

            await _gameManager.ExecuteCommand("/bm off");
            await Task.Delay(TimeSpan.FromSeconds(0.3));
            await _gameManager.ExecuteCommand("/snap motion");
            await Task.Delay(TimeSpan.FromSeconds(1.3));
            await _gameManager.ExecuteCommand("/guard motion");
            await _gameManager.ExecuteCommand($"/gs change {gearset}");
            await _gameManager.ExecuteCommand("/gaction \"Glamour Plate\"");
            await _gameManager.ExecuteCommand("/gaction \"Glamour Plate\"");
        }
    }
}
