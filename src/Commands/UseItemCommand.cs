using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Microdancer
{
    public sealed class UseItemCommand : CommandBase
    {
        private readonly IDataManager _dataManager;
        private readonly GameManager _gameManager;

        private Dictionary<uint, string>? _usableItems;

        public UseItemCommand(IDataManager dataManager, Service.Locator serviceLocator) : base(serviceLocator)
        {
            _dataManager = dataManager;
            _gameManager = serviceLocator.Get<GameManager>();

            Task.Run(
                () =>
                {
                    _usableItems = _dataManager.GetExcelSheet<Item>()!
                        .Where(i => i.ItemAction.RowId > 0)
                        .ToDictionary(i => i.RowId, i => i.Name.ToString().ToLowerInvariant())
                        .Concat(
                            _dataManager.GetExcelSheet<EventItem>()!
                                .Where(i => i.Action.RowId > 0)
                                .ToDictionary(i => i.RowId, i => i.Name.ToString().ToLowerInvariant())
                        )
                        .ToDictionary(kv => kv.Key, kv => kv.Value);
                }
            );
        }

        [Command("useitem", "item", HelpMessage = "Uses an item by name or ID. Only available out of combat.")]
        public async Task UseItem(string item)
        {
            if (_gameManager.IsInCombatOrPvP)
            {
                PrintError("Not supported while in combat or PvP.");
                return;
            }

            if (_usableItems == null)
            {
                PrintError("Not yet initialized - try opening your inventory and using this command again.");
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
                while (true)
                {
                    var t = DateTime.Now;
                    if (id < 2_000_000)
                    {
                        var actionID = _gameManager.GetSpellIdForAction(ActionType.Item, id);
                        if (actionID == 0)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(10));
                            continue;
                        }

                        if (DateTime.Now - t > TimeSpan.FromSeconds(1))
                        {
                            PrintError($"Unable to successfully use item \"{id}\"");
                            return;
                        }
                    }

                    _gameManager.UseItem(id, 9999, 0, 0);
                    break;
                }
            }
            else
            {
                PrintError("Invalid item specified.");
                return;
            }
        }
    }
}
