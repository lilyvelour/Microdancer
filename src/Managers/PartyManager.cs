using System;
using System.Linq;
using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;
using FFXIVClientStructs.FFXIV.Client.UI;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using System.Text;

namespace Microdancer
{
    public struct PartyMember
    {
        public PartyMember(string name, string world)
        {
            Name = name;
            World = world;
        }

        public string Name { get; }
        public string World { get; }
    }

    // https://github.com/SyntaxVoid/FFLogsPartyLookup/blob/e2a3174bed5ef1e9277ef796e9b3648559780764/FFLogsPartyLookup/PartyHandler.cs
    public sealed unsafe class PartyManager
    {
        // Somewhat abstractifies the interface needed to get a list of party members
        // This is not as straightforward as it seems since the game's memory treats
        // normal parties, solo parties, trust parties, light parties, full parties,
        // cross world parties, and any other parties you can think of, a bit
        // differently and special considerations need to be made for a couple of the
        // cases. To get a list of members from whatever party you're in, just call
        // PartyManager.getInfoFromParty()
        // which will delegate the fetching of info to one of several internal
        // methods depending on the party type (which you don't need to worry about)
        private delegate IntPtr InfoProxyCrossRealm_GetPtr();
        public delegate byte GetCrossRealmPartySize();

        private readonly InfoProxyCrossRealm* _infoProxyCrossRealm;

        private readonly IDataManager _dataManager;
        private readonly IGameGui _gameGui;
        private readonly IClientState _clientState;
        private readonly IPartyList _partyList;

        public PartyManager(
            IDataManager dataManager,
            IGameGui gameGui,
            IClientState clientState,
            IPartyList partyList,
            Service.Locator _
        )
        {
            _dataManager = dataManager;
            _gameGui = gameGui;
            _clientState = clientState;
            _partyList = partyList;
            _infoProxyCrossRealm = InfoProxyCrossRealm.Instance();
        }

        private string ByteToWorld(byte worldByte)
        {
            var worldSheet = _dataManager.GetExcelSheet<World>();
            var world = worldSheet?.GetRow(worldByte);

            if (world != null)
            {
                return world.Name.ToString();
            }
            else
            {
                return $"UnknownWorldForByteID={worldByte}";
            }
        }

        private int GetPartyType()
        {
            try
            {
                // Gets the type of party by inspecting the _PartyList object
                // Returns:
                //     0: Player is in a solo party
                //     1: Player is in a normal party (non-cross world)
                //     2: Player is in a cross-world party
                //     3: Player is in an alliance group
                //    -1: Player is in none of the above. Trust party maybe?
                var pList = (AddonPartyList*)_gameGui.GetAddonByName("_PartyList", 1);
                var pTypeNode = pList->PartyTypeTextNode;
                string pType = pTypeNode->NodeText.ToString();
                switch (pType)
                {
                    case "Solo":
                        return 0;
                    case "Party":
                    case "Light Party":
                    case "Full Party":
                        return 1;
                    case "Cross-world Party":
                        return 2;
                    case "Alliance A":
                    case "Alliance B":
                    case "Alliance C":
                    case "Alliance D":
                    case "Alliance E":
                    case "Alliance F":
                    case "Alliance G":
                    case "Alliance H":
                    case "Alliance I":
                        return 3;
                    default:
                        Microdancer.PluginLog.Debug($"PartyLookup: Warning (Unexpected party type): {pType}");
                        return -1;
                }
            }
            catch (Exception e)
            {
                Microdancer.PluginLog.Error(e, e.Message);
                return -1;
            }
        }

        private List<PartyMember> GetInfoFromSoloParty()
        {
            var output = new List<PartyMember>();
            var localPlayer = _clientState.LocalPlayer;
            if (localPlayer == null)
            {
                return output;
            }

            var localPlayerName = localPlayer.Name.ToString();
            var localPlayerWorld = ByteToWorld((byte)localPlayer.HomeWorld.Id);
            var localPlayerInfo = new PartyMember(localPlayerName, localPlayerWorld);
            output.Add(localPlayerInfo);

            return output;
        }

        private List<PartyMember> GetInfoFromNormalParty()
        {
            // Generates a list of playerInfo objects from the game's memory
            // assuming the party is a normal party (light/full/etc.)
            string tempName;
            string tempWorld;
            var output = new List<PartyMember>();
            var pCount = _partyList.Length;

            //int i=0;
            for (int i = 0; i < pCount; i++)
            {
                var memberPtr = _partyList.GetPartyMemberAddress(i);
                var member = _partyList.CreatePartyMemberReference(memberPtr);
                if (member == null)
                {
                    continue;
                }

                tempName = member.Name.ToString();
                tempWorld = ByteToWorld((byte)member.World.Id);
                output.Add(new(tempName, tempWorld));
            }
            return output;
        }

        private List<PartyMember> GetInfoFromCrossWorldParty()
        {
            // Generates a list of playerInfo objects from the game's memory
            // assuming the party is a cross-world party
            var output = new List<PartyMember>();
            const int maxNameLength = 30;
            const int cwPartyIndex = 0;

            var cwPartyCount = InfoProxyCrossRealm.GetGroupMemberCount(cwPartyIndex);

            for (var i = 0; i < cwPartyCount; ++i)
            {
                var groupMember = InfoProxyCrossRealm.GetGroupMember((uint)i, cwPartyIndex);

                fixed (byte* nameRawBytes = &groupMember->Name.GetPinnableReference())
                {
                    var nameRaw = Encoding.UTF8.GetString(nameRawBytes, maxNameLength);
                    var name = new string(nameRaw.TakeWhile(chr => chr > 0).ToArray());
                    var homeWorld = ByteToWorld((byte)groupMember->HomeWorld);

                    if (string.IsNullOrWhiteSpace(homeWorld))
                    {
                        Microdancer.PluginLog.Info($"Unable to parse home world for party member '{name}'");
                        continue;
                    }

                    var partyMember = new PartyMember(name, homeWorld);
                    output.Add(partyMember);
                }
            }

            return output;
        }

        private List<PartyMember> GetInfoFromAllianceParty()
        {
            // Generates a list of playerInfo objects from the game's memory
            // assuming the party is an alliance party. Alliance parties are a
            // bit funky though... If you're in the overworld, they work like a
            // cross world party and if you're in a duty they work like a normal
            // party. We can check the PartyList.Length attribute to figure out
            // which is which. If we're in a crossworld alliance party (overworld),
            // then PartyList.Length will be 0, otherwise it will be some non-
            // negative number. We are also assuming the party is either Alliance A,
            // Alliance B, ... etc. already.
            if (_partyList.Length == 0) // Then we're in the overworld
            {
                return GetInfoFromCrossWorldParty();
            }
            else // Then we're in a duty
            {
                return GetInfoFromNormalParty();
            }
        }

        public List<PartyMember> GetInfoFromParty()
        {
            // This is the outward facing method you can interact with if you don't
            // want to deal with figuring out what kind of party you're in yourself.
            // Returns null if an unknown party type is detected.

            // Determine the party type
            var pType = GetPartyType();
            return pType switch
            {
                // Solo Party
                0 => GetInfoFromSoloParty(),
                // Normal non-cross world party
                1 => GetInfoFromNormalParty(),
                // Cross World party
                2 => GetInfoFromCrossWorldParty(),
                // Alliance Party
                3 => GetInfoFromAllianceParty(),
                // Fail
                _ => new List<PartyMember>(),
            };
        }
    }
}
