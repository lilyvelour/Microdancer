using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Dalamud.Logging;
using Dalamud.Game.ClientState.Party;
using Lumina.Excel.GeneratedSheets;
using FFXIVClientStructs.FFXIV.Client.UI;
using Dalamud.Game;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.ClientState;
using Dalamud.IoC;

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
    [PluginInterface]
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

        private readonly InfoProxyCrossRealm_GetPtr InfoProxyCrossRealm_GetPtrDelegate;
        private readonly GetCrossRealmPartySize getCrossRealmPartySize;

        private readonly DataManager _dataManager;
        private readonly GameGui _gameGui;
        private readonly ClientState _clientState;
        private readonly PartyList _partyList;

        public PartyManager(
            SigScanner sigScanner,
            DataManager dataManager,
            GameGui gameGui,
            ClientState clientState,
            PartyList partyList
        )
        {
            _dataManager = dataManager;
            _gameGui = gameGui;
            _clientState = clientState;
            _partyList = partyList;

            // This needs to be started when the plugin is loaded to identify the
            // location in memory where the cross-world party exists. This will
            // probably break if SE ever changes the offsets or the layout
            // of the CrossRealmGroup class.
            // src: gist.github.com/Eternita-S/c21192996d181c41740c6322f2760e16
            var ipcr_ptr = sigScanner.ScanText(Signatures.InfoProxyCrossRealm);
            InfoProxyCrossRealm_GetPtrDelegate = Marshal.GetDelegateForFunctionPointer<InfoProxyCrossRealm_GetPtr>(
                ipcr_ptr
            );
            var gcrps_ptr = sigScanner.ScanText(Signatures.GetCrossRealmPartySize);
            getCrossRealmPartySize = Marshal.GetDelegateForFunctionPointer<GetCrossRealmPartySize>(gcrps_ptr);
        }

        private PartyMember GetCrossRealmPlayer(int index)
        {
            // Utilizes the results of the SigScanner from Init() to locate
            // information about a specific crossworld player and build a
            // playerInfo object based on the provided index. If there's any error,
            // returns a default playerInfo object instead
            try
            {
                var playerPtr = InfoProxyCrossRealm_GetPtrDelegate() + 0x3c2 + 0x50 * index;
                var playerName = Marshal.PtrToStringUTF8(playerPtr + 0x8) ?? "NotFound";
                var world = WorldNameFromByte(*(byte*)playerPtr);
                return new PartyMember(playerName, world);
            }
            catch
            {
                return new PartyMember("NotFound", "NotFound");
            }
        }

        private string ClassJobFromByte(byte classJobByte)
        {
            // Given a classJob byte, returns a string correlating to the actual
            // classJob. This isn't really used since in regular parties, the job
            // is not known to the client unless you are in the same zone as the
            // party member. In that case, the classJob ends up resolving to the
            // default "adventurer". The actual information is *somewhere* in the
            // memory since you can see the job in the party list... but I don't
            // want to deal with that :3 + I don't really care that much.
#pragma warning disable 8632
            var ClassJobSheet = _dataManager.GetExcelSheet<ClassJob>();
            var ClassJobs = ClassJobSheet?.ToArray();
            if (ClassJobs != null) // We found the ClassJobs!
            {
                ClassJob? ClassJob = Array.Find(ClassJobs, x => x.RowId == classJobByte);
                if (ClassJob != null)
                {
                    return ClassJob.Name.ToString();
                }
                else
                {
                    return $"UnknownClassJobForByteID={classJobByte}";
                }
            }
            else
            {
                return "UnableToFindWorld";
            }
#pragma warning restore 8632
        }

        private string WorldNameFromByte(byte worldByte)
        {
            // Given a byte for a specific world (SE decides how these are mapped),
            // this method will search the Worlds excel sheet (provided by Dalamud)
            // to resolve to a string of the actual world name. This is basically the
            // same method as classJobFromByte but uses World instead of classJob and
            // I'm sure some kind of template could combine these two, but I'm not
            // familiar enough with templates for a robust solution.
#pragma warning disable 8632
            var worldSheet = _dataManager.GetExcelSheet<World>();
            var worlds = worldSheet?.ToArray();
            if (worlds != null) // We found the worlds!
            {
                var world = Array.Find(worlds, x => x.RowId == worldByte);
                if (world != null)
                {
                    return world.Name.ToString();
                }
                else
                {
                    return $"UnknownWorldForByteID={worldByte}";
                }
            }
            else
            {
                return "UnableToFindWorld";
            }
#pragma warning restore 8632
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
                        PluginLog.Debug($"PartyLookup: Warning (Unexpected party type): {pType}");
                        return -1;
                }
            }
            catch (Exception e)
            {
                PluginLog.Error(e, e.Message);
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
            var localPlayerWorld = WorldNameFromByte((byte)localPlayer.HomeWorld.Id);
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
                tempWorld = WorldNameFromByte((byte)member.World.Id);
                output.Add(new PartyMember(tempName, tempWorld));
            }
            return output;
        }

        private List<PartyMember> GetInfoFromCrossWorldParty()
        {
            // Generates a list of playerInfo objects from the game's memory
            // assuming the party is a cross-world party
            var output = new List<PartyMember>();
            var pSize = getCrossRealmPartySize();
            for (int i = 0; i < pSize; i++)
            {
                output.Add(GetCrossRealmPlayer(i));
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
