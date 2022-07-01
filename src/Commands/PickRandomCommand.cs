using System;

namespace Microdancer
{
    public sealed class PickRandomCommand : CommandBase
    {
        private readonly GameManager _gameManager;
        private readonly Random _random = new();

        public PickRandomCommand(Service.Locator serviceLocator) : base(serviceLocator)
        {
            _gameManager = serviceLocator.Get<GameManager>();
        }

        [Command(
            "pickrandom",
            "prandom",
            HelpMessage = "Syntax: /pickrandom <command1>;<command2...> - performs one of the commands at random",
            Raw = true
        )]
        public async void Random(string commandStr)
        {
            if (_gameManager.IsInCombatOrPvP)
            {
                PrintError("Not supported while in combat or PvP.");
                return;
            }

            var commands = commandStr.Split(";");

            if (commands.Length > 0)
            {
                var index = _random.Next(0, commands.Length);
                await _gameManager.ExecuteCommand(commands[index]);
            }
        }
    }
}
