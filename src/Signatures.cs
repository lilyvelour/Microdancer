// Sources
// https://raw.githubusercontent.com/UnknownX7/OOBlugin/master/Game.cs
// https://gist.github.com/Eternita-S/c21192996d181c41740c6322f2760e16
// https://github.com/BardMusicPlayer/Hypnotoad-Plugin/blob/c690b20c0f090c5a570673c3063c0459116c60b7/HypnotoadPlugin/Offsets/Offsets.cs

namespace Microdancer
{
    public static class Signatures
    {
        // void ProcessChatBox(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4)
        // https://raw.githubusercontent.com/Ottermandias/GatherBuddy/c86160592f1a31b2b92f9f451157e96a795b35d2/GatherBuddy/SeFunctions/ProcessChatBox.cs
        public const string ProcessChatBox = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B F2 48 8B F9 45 84 C9";

        // void DoEmote(IntPtr agent, uint emoteID, long a3, bool a4, bool a5)
        public const string DoEmote = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? B8 0A 00 00 00";

        // byte
        public const string ActionCommandRequestType = "02 00 00 00 41 8B D7 89";

        public const string KeyStates = "4C 8D 05 ?? ?? ?? ?? 44 8B 0D";

        public const string KeyStateIndexArray = "0F B6 94 33 ?? ?? ?? ?? 84 D2";

        public const string DoPerformAction = "48 89 6C 24 10 48 89 74 24 18 57 48 83 EC ?? 48 83 3D ?? ?? ?? ?? ?? 41 8B E8"; // TODO
        public const string PerformanceStructPtr = "48 8B C2 0F B6 15 ?? ?? ?? ?? F6 C2 01"; // TODO
    }
}
