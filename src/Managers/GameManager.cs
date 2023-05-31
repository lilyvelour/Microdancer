using System;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using XIVFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Threading.Channels;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Microdancer
{
    // https://raw.githubusercontent.com/UnknownX7/OOBlugin/master/Game.cs
    // https://github.com/Ottermandias/AutoVisor/blob/c0b22286119d6ff207715c3dd1726137bb146863/SeFunctions/CPoseSettings.cs
    public unsafe class GameManager : IDisposable
    {
        private readonly GameGui _gameGui;
        private readonly SigScanner _sigScanner;
        private readonly ClientState _clientState;
        private readonly Framework _framework;
        private readonly Condition _condition;
        private readonly Channel<(string command, byte actionCommandRequestType)> _channel =
            Channel.CreateUnbounded<(string, byte)>();

        private readonly HashSet<Keys> _heldKeys = new();

        private bool _disposedValue;

        public GameManager(
            GameGui gameGui,
            SigScanner sigScanner,
            ClientState clientState,
            Framework framework,
            Condition condition,
            Service.Locator _
        )
        {
            _gameGui = gameGui;
            _sigScanner = sigScanner;
            _clientState = clientState;
            _framework = framework;
            _condition = condition;

            _framework.Update += Update;

            Initialize();
        }

        private delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);

        private ProcessChatBoxDelegate? ProcessChatBox;
        private IntPtr uiModule = IntPtr.Zero;
        private IntPtr walkingBoolPtr = IntPtr.Zero;

        public string? PlayerName => _clientState.LocalPlayer?.Name?.ToString();
        public bool IsLoggedIn => _clientState.IsLoggedIn;

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

        public bool IsInCombatOrPvP
        {
            get { return _condition[ConditionFlag.InCombat] || GameMain.IsInPvPArea(); }
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

        public ValueTask ExecuteCommand(string command, byte actionCommandRequestType = 2)
        {
            return _channel.Writer.WriteAsync((command, actionCommandRequestType));
        }

        private int* _keyStates;
        private byte* _keyStateIndexArray;

        public byte GetKeyStateIndex(Keys key)
        {
            var keyCode = (int)key;

            return keyCode is >= 0 and < 240 ? _keyStateIndexArray[keyCode] : (byte)0;
        }

        private ref int GetKeyState(int key) => ref _keyStates[key];

        public bool SendKeyHold(Keys key, bool releaseNextFrame = true)
        {
            if (releaseNextFrame)
            {
                _heldKeys.Add(key);
            }

            var stateIndex = GetKeyStateIndex(key);
            if (stateIndex <= 0)
            {
                return false;
            }

            GetKeyState(stateIndex) |= 1;
            return true;
        }

        public bool SendKey(Keys key)
        {
            var stateIndex = GetKeyStateIndex(key);
            if (stateIndex <= 0)
            {
                return false;
            }

            GetKeyState(stateIndex) |= 6;
            return true;
        }

        public bool SendKeyRelease(Keys key)
        {
            var stateIndex = GetKeyStateIndex(key);
            if (stateIndex <= 0)
            {
                return false;
            }

            GetKeyState(stateIndex) &= ~1;
            return true;
        }

        private delegate void DoPerformActionDelegate(IntPtr performInfoPtr, uint instrumentId, int a3 = 0);
        private DoPerformActionDelegate? DoPerformAction;
        private IntPtr PerformanceStruct;

        public void OpenInstrument(uint instrumentId)
        {
            DoPerformAction?.Invoke(PerformanceStruct, instrumentId);
        }

        private void Initialize()
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
                var agentModule = XIVFramework.Instance()->GetUiModule()->GetAgentModule();

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
                    itemContextMenuAgent = (IntPtr)agentModule->GetAgentByInternalID((uint)AgentId.InventoryContext); // TODO: May break in 6.2

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
                _keyStates = (int*)_sigScanner.GetStaticAddressFromSig(Signatures.KeyStates);
                _keyStateIndexArray = (byte*)(
                    _sigScanner.Module.BaseAddress + *(int*)(_sigScanner.ScanModule(Signatures.KeyStateIndexArray) + 4)
                );
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                PluginLog.LogWarning("Failed to load /sendkey");
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
                DoPerformAction = Marshal.GetDelegateForFunctionPointer<DoPerformActionDelegate>(
                    _sigScanner.ScanText(Signatures.DoPerformAction)
                );
                PerformanceStruct = _sigScanner
                    .GetStaticAddressFromSig(Signatures.PerformanceStructPtr);
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, e.Message);
                PluginLog.LogWarning("Failed to load DoPerformAction");
            }
        }

        private void ExcecuteCommandImmediate(string command)
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
                PluginLog.LogError("ExecuteCommand: Failed injecting command");
            }
        }

        private void Update(Framework _)
        {
            if (_heldKeys.Count > 0)
            {
                foreach (var key in _heldKeys)
                {
                    SendKeyRelease(key);
                }

                _heldKeys.Clear();
            }

            if (_clientState.LocalPlayer == null)
            {
                return;
            }

            while (_channel.Reader.TryRead(out var cmd))
            {
                ActionCommandRequestType = cmd.actionCommandRequestType;
                ExcecuteCommandImmediate(cmd.command);
                ActionCommandRequestType = 0;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                _framework.Update -= Update;
            }

            _disposedValue = true;
        }
    }
}
