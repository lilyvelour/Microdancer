// Sources
// https://raw.githubusercontent.com/UnknownX7/OOBlugin/master/Game.cs
// https://github.com/Ottermandias/AutoVisor/blob/master/SeFunctions/CPoseSettings.cs
// https://gist.github.com/Eternita-S/c21192996d181c41740c6322f2760e16

namespace Microdancer
{
    public static class Signatures
    {
        // void ProcessChatBox(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4)
        public const string ProcessChatBox = "48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9";

        // bool*
        public const string IsWalking = "88 83 ?? ?? ?? ?? 0F B6 05 ?? ?? ?? ?? 88 83";

        // void DoEmote(IntPtr agent, uint emoteID, long a3, bool a4, bool a5)
        public const string DoEmote = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? B8 0A 00 00 00";

        // useItem(
        //     IntPtr itemContextMenuAgent,
        //     uint itemID,
        //     uint inventoryPage,
        //     uint inventorySlot,
        //     short a5)
        public const string UseItem = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 41 B0 01 BA 13 00 00 00";

        // GetActionId(uint actionType, uint actionCategoryID)
        public const string GetActionId = "E8 ?? ?? ?? ?? 44 8B 4B 2C";

        // byte
        public const string ActionCommandRequestType = "02 00 00 00 41 8B D7 89";

        // byte*
        public const string CPoseSettings = "48 8D 05 ?? ?? ?? ?? 0F B6 1C 38";

        // int InfoProxyCrossRealm()
        public const string InfoProxyCrossRealm = "48 8B 05 ?? ?? ?? ?? C3 CC CC CC CC CC CC CC CC 40 53 41 57";

        // byte GetCrossRealmPartySize()
        public const string GetCrossRealmPartySize = "48 83 EC 28 E8 ?? ?? ?? ?? 84 C0 74 3C";
    }
}
