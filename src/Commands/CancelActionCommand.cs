using System;
using System.Threading.Tasks;

namespace Microdancer
{
    public sealed class CancelActionCommand : CommandBase
    {
        private readonly GameManager _gameManager;

        public CancelActionCommand(Service.Locator serviceLocator) : base(serviceLocator)
        {
            _gameManager = serviceLocator.Get<GameManager>();
        }

        [Command(
            "cancelaction",
            "acancel",
            HelpMessage = "Cancel a channeled action or end animation of a persistent emote. Must have access to glamour plates."
        )]
        public async Task CancelAction()
        {
            await _gameManager.ExecuteCommand("/gaction \"Glamour Plate\"");
            await _gameManager.ExecuteCommand("/gaction \"Glamour Plate\"");
        }
    }
}
