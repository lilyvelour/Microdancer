using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Logging;

namespace Microdancer
{
    public sealed class UseItemCommand : CommandBase
    {
        private readonly DataManager _dataManager;
        private readonly Condition _condition;
        private readonly GameManager _gameManager;

        private Dictionary<uint, string>? _usableItems;

        public UseItemCommand(
            CommandManager commandManager,
            Configuration configuration,
            DataManager dataManager,
            Condition condition,
            GameManager gameManager) : base(commandManager, configuration)
        {
            _dataManager = dataManager;
            _condition = condition;
            _gameManager = gameManager;

            Task.Run(async () =>
            {
                while (_dataManager.IsDataReady != true)
                {
                    await Task.Delay(1000);
                }

                _usableItems = _dataManager
                    .GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()!
                    .Where(i => i.ItemAction.Row > 0)
                    .ToDictionary(i => i.RowId, i => i.Name.ToString().ToLower())
                    .Concat(
                        _dataManager
                            .GetExcelSheet<Lumina.Excel.GeneratedSheets.EventItem>()!
                            .Where(i => i.Action.Row > 0)
                            .ToDictionary(i => i.RowId, i => i.Name.ToString().ToLower()))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            });
        }

        [Command("useitem", "item", HelpMessage = "Uses an item by name or ID. Only available out of combat.")]
        public void UseItem(string item)
        {
            if (_condition[ConditionFlag.InCombat])
            {
                PluginLog.LogError("/useitem is not supported while in combat.");
                return;
            }

            if (_usableItems == null)
            {
                PluginLog.LogError("/useitem is not yet initialized.");
                return;
            }

            if (!uint.TryParse(item, out var id))
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    var name = item.Replace("\uE03C", string.Empty); // Remove HQ Symbol
                    var useHQ = item != name;
                    name = name.ToLower().Trim(' ');
                    try
                    {
                        id = _usableItems.First(i => i.Value == name).Key + (uint)(useHQ ? 1_000_000 : 0);
                    }
                    catch { }
                }
            }
            else
            {
                if (!_usableItems.ContainsKey(id is >= 1_000_000 and < 2_000_000 ? id - 1_000_000 : id))
                {
                    id = 0;
                }
            }

            if (id > 0)
            {
                _gameManager.UseItem?.Invoke(_gameManager.itemContextMenuAgent, id, 9999, 0, 0);
            }
            else
            {
                PluginLog.LogError("Invalid item specified.");
            }
        }
    }
}