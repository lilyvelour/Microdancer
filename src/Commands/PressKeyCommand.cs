using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microdancer
{
    public sealed class PressKeyCommand : CommandBase
    {
        private readonly GameManager _gameManager;

        public PressKeyCommand(GameManager gameManager) : base()
        {
            _gameManager = gameManager;
        }

        [Command(
            "presskey",
            HelpMessage = "Send a key press and release to the game using virtual key code or virtual key name. Accepts an optional duration in seconds."
        )]
        public void PressKey(string keyExpression, float holdDuration = 0)
        {
            Task.Run(() => PressKeyImpl(keyExpression, TimeSpan.FromSeconds(holdDuration)));
        }

        private async Task PressKeyImpl(string keyExpression, TimeSpan holdDuration = default)
        {
            if (_gameManager.IsInCombatOrPvP)
            {
                PrintError("Not supported while in combat or PvP.");
                return;
            }

            if (keyExpression == "help")
            {
                foreach (var key in (Keys[])Enum.GetValues(typeof(Keys)))
                {
                    if (_gameManager.GetKeyStateIndex(key) > 0)
                    {
                        Print($"{key} = {(int)key}");
                    }
                }
                return;
            }

            var regex = Regex.Match(keyExpression, @"^([+^%]*)(.+)");
            if (!regex.Success)
            {
                PrintError("Unable to parse key code.");
                return;
            }

            var mods = regex.Groups[1].Value;
            var keyStr = regex.Groups[2].Value;
            var releaseNextFrame = holdDuration.TotalMilliseconds <= 0;
            var keyStack = new Stack<Keys>();

            if (mods.Contains("+"))
            {
                _gameManager.SendKeyHold(Keys.ShiftKey, releaseNextFrame);
                keyStack.Push(Keys.ShiftKey);
            }

            if (mods.Contains("^"))
            {
                _gameManager.SendKeyHold(Keys.ControlKey, releaseNextFrame);
                keyStack.Push(Keys.ControlKey);
            }

            if (mods.Contains("%"))
            {
                _gameManager.SendKeyHold(Keys.Menu, releaseNextFrame);
                keyStack.Push(Keys.Menu);
            }

            if (!byte.TryParse(keyStr, out var keyCode) && Enum.TryParse(typeof(Keys), keyStr, true, out var keyEnum))
            {
                if (keyEnum == null)
                {
                    PrintError("Unable to parse key code.");
                    return;
                }

                keyCode = (byte)(int)keyEnum;
            }

            var keyValue = (Keys)keyCode;
            var success = releaseNextFrame ? _gameManager.SendKey(keyValue) : _gameManager.SendKeyHold(keyValue, false);
            keyStack.Push(keyValue);

            if (!success)
            {
                PrintError("Invalid key code.");
                return;
            }

            if (!releaseNextFrame)
            {
                await Task.Delay(holdDuration);

                while (keyStack.Count > 0)
                {
                    _gameManager.SendKeyRelease(keyStack.Pop());
                }
            }
        }
    }
}
