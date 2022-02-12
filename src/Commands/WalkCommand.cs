using Dalamud.Game.Command;

namespace Microdancer
{
    public sealed class WalkCommand : CommandBase
    {
        private readonly GameManager _gameManager;

        public WalkCommand(
            CommandManager commandManager,
            Configuration configuration,
            GameManager gameManager) : base(commandManager, configuration)
        {
            _gameManager = gameManager;
        }

        [Command("walk", HelpMessage = "Toggle between walk and run. Optional subcommands: [on, off]")]
        public void Walk(string toggle)
        {
            if (toggle == "on")
            {
                _gameManager.IsWalking = true;
            }
            else if (toggle == "off")
            {
                _gameManager.IsWalking = false;
            }
            else
            {
                _gameManager.IsWalking ^= true;
            }
        }
    }
}