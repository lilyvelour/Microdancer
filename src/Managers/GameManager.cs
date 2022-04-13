using System;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

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
                    _sigScanner.ScanText(Signatures.ProcessChatBox)
                );
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                PluginLog.LogWarning("Failed to load ProcessChatBox");
            }

            try
            {
                // also found around g_PlayerMoveController+523
                walkingBoolPtr = _sigScanner.GetStaticAddressFromSig(Signatures.IsWalking);
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                PluginLog.LogWarning("Failed to load /walk");
            }

            try
            {
                var agentModule = Framework.Instance()->GetUiModule()->GetAgentModule();

                try
                {
                    DoEmote = Marshal.GetDelegateForFunctionPointer<DoEmoteDelegate>(
                        _sigScanner.ScanText(Signatures.DoEmote)
                    );
                    emoteAgent = (IntPtr)agentModule->GetAgentByInternalID((uint)AgentId.Emote);
                }
                catch (Exception e)
                {
                    PluginLog.LogError(e, e.Message);
                    PluginLog.LogWarning("Failed to load /doemote");
                }

                try
                {
                    UseItem = Marshal.GetDelegateForFunctionPointer<UseItemDelegate>(
                        _sigScanner.ScanText(Signatures.UseItem)
                    );
                    itemContextMenuAgent = (IntPtr)agentModule->GetAgentByInternalID(10);

                    GetActionId = Marshal.GetDelegateForFunctionPointer<GetActionIdDelegate>(
                        _sigScanner.ScanText(Signatures.GetActionId)
                    );
                }
                catch (Exception e)
                {
                    PluginLog.LogError(e, e.Message);
                    PluginLog.LogWarning("Failed to load /useitem");
                }
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                PluginLog.LogWarning("Failed to get agent module");
            }

            // Located 1 function deep in Client__UI__Shell__ShellCommandAction_ExecuteCommand
            try
            {
                actionCommandRequestTypePtr = _sigScanner.ScanText(Signatures.ActionCommandRequestType);
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                PluginLog.LogWarning("Failed to load /qac");
            }

            try
            {
                ActionManager = (IntPtr)FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                PluginLog.LogWarning("Failed to load ActionManager");
            }

            try
            {
                CPoseSettings = _sigScanner.GetStaticAddressFromSig(Signatures.CPoseSettings);
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                PluginLog.LogWarning("Failed to load CPoseSettings");
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
