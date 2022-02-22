using System;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace Microdancer
{
    // https://raw.githubusercontent.com/UnknownX7/OOBlugin/master/Game.cs
    // https://github.com/Ottermandias/AutoVisor/blob/c0b22286119d6ff207715c3dd1726137bb146863/SeFunctions/CPoseSettings.cs
    [PluginInterface]
    public unsafe class GameManager
    {
        private readonly GameGui _gameGui;
        private readonly SigScanner _sigScanner;

        public GameManager(GameGui gameGui, SigScanner sigScanner)
        {
            _gameGui = gameGui;
            _sigScanner = sigScanner;

            Initialize();
        }

        private delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);
        private ProcessChatBoxDelegate? ProcessChatBox;
        private IntPtr uiModule = IntPtr.Zero;
        private IntPtr walkingBoolPtr = IntPtr.Zero;

        public bool IsWalking
        {
            get => walkingBoolPtr != IntPtr.Zero && *(bool*)walkingBoolPtr;
            set
            {
                if (walkingBoolPtr != IntPtr.Zero)
                {
                    *(bool*)walkingBoolPtr = value;
                    *(bool*)(walkingBoolPtr - 0x10B) = value; // Autorun
                }
            }
        }

        public IntPtr emoteAgent = IntPtr.Zero;
        public delegate void DoEmoteDelegate(IntPtr agent, uint emoteID, long a3, bool a4, bool a5);
        public DoEmoteDelegate? DoEmote;

        public IntPtr itemContextMenuAgent = IntPtr.Zero;
        public delegate void UseItemDelegate(
            IntPtr itemContextMenuAgent,
            uint itemID,
            uint inventoryPage,
            uint inventorySlot,
            short a5
        );
        public UseItemDelegate? UseItem;

        public delegate uint GetActionIdDelegate(uint actionType, uint actionCategoryID);
        public GetActionIdDelegate? GetActionId;

        public IntPtr actionCommandRequestTypePtr = IntPtr.Zero;
        public byte ActionCommandRequestType
        {
            get => *(byte*)actionCommandRequestTypePtr;
            set
            {
                if (actionCommandRequestTypePtr != IntPtr.Zero)
                    SafeMemory.WriteBytes(actionCommandRequestTypePtr, new[] { value });
            }
        }

        public IntPtr ActionManager = IntPtr.Zero;
        public ref bool IsQueued => ref *(bool*)(ActionManager + 0x68);
        public ref uint QueuedActionType => ref *(uint*)(ActionManager + 0x6C);
        public ref uint QueuedAction => ref *(uint*)(ActionManager + 0x70);
        public ref long QueuedTarget => ref *(long*)(ActionManager + 0x78);
        public ref uint QueuedUseType => ref *(uint*)(ActionManager + 0x80);
        public ref uint QueuedPVPAction => ref *(uint*)(ActionManager + 0x84);

        public IntPtr CPoseSettings = IntPtr.Zero;

        public void Initialize()
        {
            try
            {
                uiModule = _gameGui.GetUIModule();
                ProcessChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(
                    _sigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9")
                );
            }
            catch
            {
                PluginLog.LogError("Failed to load /qexec");
            }

            try
            {
                // also found around g_PlayerMoveController+523
                walkingBoolPtr = _sigScanner.GetStaticAddressFromSig("88 83 ?? ?? ?? ?? 0F B6 05 ?? ?? ?? ?? 88 83");
            }
            catch
            {
                PluginLog.LogError("Failed to load /walk");
            }

            try
            {
                var agentModule = Framework.Instance()->GetUiModule()->GetAgentModule();

                try
                {
                    DoEmote = Marshal.GetDelegateForFunctionPointer<DoEmoteDelegate>(
                        _sigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? B8 0A 00 00 00")
                    );
                    emoteAgent = (IntPtr)agentModule->GetAgentByInternalID(19);
                }
                catch
                {
                    PluginLog.LogError("Failed to load /doemote");
                }

                try
                {
                    UseItem = Marshal.GetDelegateForFunctionPointer<UseItemDelegate>(
                        _sigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 41 B0 01 BA 13 00 00 00")
                    );
                    itemContextMenuAgent = (IntPtr)agentModule->GetAgentByInternalID(10);

                    GetActionId = Marshal.GetDelegateForFunctionPointer<GetActionIdDelegate>(
                        _sigScanner.ScanText("E8 ?? ?? ?? ?? 44 8B 4B 2C")
                    );
                }
                catch
                {
                    PluginLog.LogError("Failed to load /useitem");
                }
            }
            catch
            {
                PluginLog.LogError("Failed to get agent module");
            }

            // Located 1 function deep in Client__UI__Shell__ShellCommandAction_ExecuteCommand
            try
            {
                actionCommandRequestTypePtr = _sigScanner.ScanText("02 00 00 00 41 8B D7 89");
            }
            catch
            {
                PluginLog.LogError("Failed to load /qac");
            }

            try
            {
                ActionManager = (IntPtr)FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
            }
            catch
            {
                PluginLog.LogError("Failed to load ActionManager");
            }

            try
            {
                CPoseSettings = _sigScanner.GetStaticAddressFromSig("48 8D 05 ?? ?? ?? ?? 0F B6 1C 38");
            }
            catch
            {
                PluginLog.LogError("Failed to load CPoseSettings");
            }
        }

        public void ExecuteCommand(string command)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(command + "\0");
                var memStr = Marshal.AllocHGlobal(0x18 + bytes.Length);

                Marshal.WriteIntPtr(memStr, memStr + 0x18); // String pointer
                Marshal.WriteInt64(memStr + 0x8, bytes.Length); // Byte capacity (unused)
                Marshal.WriteInt64(memStr + 0x10, bytes.Length); // Byte length
                Marshal.Copy(bytes, 0, memStr + 0x18, bytes.Length); // String

                ProcessChatBox?.Invoke(uiModule, memStr, IntPtr.Zero, 0);

                Marshal.FreeHGlobal(memStr);
            }
            catch
            {
                PluginLog.LogError("Failed injecting command");
            }
        }
    }
}
