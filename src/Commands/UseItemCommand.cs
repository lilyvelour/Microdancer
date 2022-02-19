using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
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
            DataManager dataManager,
            Condition condition,
            GameManager gameManager) : base()
        {
            _dataManager = dataManager;
            _condition = condition;
            _gameManager = gameManager;

            Task.Run(async () =>
            {
                while (_dataManager.IsDataReady != true)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(10));
                }

                _usableItems = _dataManager
                    .GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()!
                    .Where(i => i.ItemAction.Row > 0)
                    .ToDictionary(i => i.RowId, i => i.Name.ToString().ToLowerInvariant())
                    .Concat(
                        _dataManager
                            .GetExcelSheet<Lumina.Excel.GeneratedSheets.EventItem>()!
                            .Where(i => i.Action.Row > 0)
                            .ToDictionary(i => i.RowId, i => i.Name.ToString().ToLowerInvariant()))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            });
        }

        [Command("useitem", "item", HelpMessage = "Uses an item by name or ID. Only available out of combat.")]
        public async Task UseItem(string item)
        {
            if (_condition[ConditionFlag.InCombat])
            {
                PrintError("useitem", "Not supported while in combat.");
                return;
            }

            if (_usableItems == null || _gameManager.GetActionId == null || _gameManager.UseItem == null)
            {
                PrintError("useitem", "Not yet initialized - try opening your inventory and using this command again.");
                return;
            }

            if (!uint.TryParse(item, out var id))
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    var name = item.Replace("\uE03C", string.Empty); // Remove HQ Symbol
                    var useHQ = item != name;
                    name = name.ToLowerInvariant().Trim(' ');
                    try
                    {
                        id = _usableItems.First(i => i.Value == name).Key + (uint)(useHQ ? 1_000_000 : 0);
                    }
                    catch { }
                }
            }

            if (id > 0 && _usableItems.ContainsKey(id is >= 1_000_000 and < 2_000_000 ? id - 1_000_000 : id))
            {
                do
                {
                    var t = DateTime.Now;
                    if (id < 2_000_000)
                    {
                        var actionID = _gameManager.GetActionId?.Invoke(2, id) ?? 0;
                        if (actionID == 0)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(10));
                            continue;
                        }

                        if (DateTime.Now - t > TimeSpan.FromSeconds(1))
                        {
                            PrintError("useitem", $"Unable to successfully use item \"{id}\"");
                            return;
                        }
                    }

                    _gameManager.ActionCommandRequestType = 0;
                    _gameManager.UseItem?.Invoke(_gameManager.itemContextMenuAgent, id, 9999, 0, 0);
                    _gameManager.ActionCommandRequestType = 2;
                    break;
                }
                while (true);
            }
            else
            {
                PrintError("useitem", "Invalid item specified.");
                return;
            }
        }
    }
}