using Dalamud.Game.Command;

namespace Microdancer
{
    public sealed class WalkCommand : CommandBase
    {
        private readonly GameManager _gameManager;

        public WalkCommand(Service.Locator serviceLocator) : base(serviceLocator)
        {
            _gameManager = serviceLocator.Get<GameManager>();
        }

        [Command("walk", HelpMessage = "Toggle between walk and run. Optional subcommands: [on, off]")]
        public void Walk(bool? isWalking = null)
        {
            if (isWalking != null)
            {
                _gameManager.IsWalking = isWalking.Value;
            }
            else
            {
                _gameManager.IsWalking ^= true;
            }
        }
    }
}
