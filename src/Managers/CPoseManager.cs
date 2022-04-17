using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.IoC;
using Dalamud.Logging;

namespace Microdancer
{
    [PluginInterface]
    public class CPoseManager
    {
        public enum PoseType
        {
            Invalid = -1,
            Stand = 0,
            Weapon = 1,
            Sit = 2,
            GroundSit = 3,
            Doze = 4,
            Parasol = 5,
        }

        private readonly GameManager _gameManager;
        private readonly ClientState _clientState;
        private readonly Condition _condition;

        public CPoseManager(GameManager gameManager, ClientState clientState, Condition condition)
        {
            _gameManager = gameManager;
            _clientState = clientState;
            _condition = condition;
        }

        public PoseType GetPoseTypeFromName(string name)
        {
            switch (name)
            {
                case "stand":
                    return PoseType.Stand;
                case "weapon":
                    return PoseType.Weapon;
                case "sit":
                    return PoseType.Sit;
                case "groundsit":
                    return PoseType.GroundSit;
                case "doze":
                    return PoseType.Doze;
                case "parasol":
                    return PoseType.Parasol;
                default:
                    return PoseType.Invalid;
            }
        }

        public unsafe PoseType GetCurrentPoseType()
        {
            var player = _clientState.LocalPlayer;
            if (player == null)
            {
                return PoseType.Invalid;
            }

            var playerPointer = player.Address;

            if (_condition[ConditionFlag.UsingParasol]) // Using a parasol
            {
                return PoseType.Parasol;
            }

            if (player.StatusFlags.HasFlag(StatusFlags.WeaponOut))
            {
                return PoseType.Weapon;
            }

            var ptr = (byte*)playerPointer.ToPointer();
            var seatingState = *(ptr + Offsets.Character.SeatingState); // Sitting or dozing
            switch (seatingState)
            {
                case 1:
                    return PoseType.GroundSit;
                case 2:
                    return PoseType.Sit;
                case 3:
                    return PoseType.Doze;
            }

            return PoseType.Stand;
        }

        public int GetPoseCount(PoseType poseType)
        {
            switch (poseType)
            {
                case PoseType.Stand:
                    return 7;
                case PoseType.Weapon:
                    return 2;
                case PoseType.Sit:
                    return 3;
                case PoseType.GroundSit:
                    return 4;
                case PoseType.Doze:
                    return 3;
                case PoseType.Parasol:
                    return 3;
                default:
                    return 0;
            }
        }

        public void SetPose(PoseType newPoseType, byte newPoseIndex)
        {
            var currentPoseType = GetCurrentPoseType();
            var currentPoseIndex = GetPose(currentPoseType);

            if (currentPoseType == newPoseType)
            {
                if (newPoseIndex == GetCurrentPose())
                {
                    if (currentPoseIndex != newPoseIndex)
                    {
                        WritePoseAndLog();
                    }
                }
                else
                {
                    Task.Run(CyclePosesAndLog);
                }
            }
            else if (currentPoseIndex != newPoseIndex)
            {
                WritePoseAndLog();
            }

            void WritePoseAndLog()
            {
                WritePose(newPoseType, newPoseIndex);

                PluginLog.LogDebug(
                    "Overwrote {currentPoseIndex} with {newPoseIndex} for {newPoseType:l}, currently in {currentPoseType:l}.",
                    currentPoseIndex,
                    newPoseIndex,
                    newPoseType,
                    currentPoseType
                );
            }

            void CyclePosesAndLog()
            {
                var currentPoseCount = GetPoseCount(currentPoseType);

                if (currentPoseCount <= 0)
                {
                    return;
                }

                var previousPose = newPoseIndex == 0 ? currentPoseCount - 1 : newPoseIndex - 1;
                WritePose(newPoseType, (byte)previousPose);

                var currentActorPoseIndex = GetCurrentPose();

                if (currentPoseCount <= 0 || currentActorPoseIndex < 0)
                {
                    return;
                }

                int i;
                for (i = 0; i < currentPoseCount - 1; ++i)
                {
                    currentActorPoseIndex = GetCurrentPose();
                    if (currentActorPoseIndex == newPoseIndex)
                    {
                        break;
                    }

                    _gameManager.ExecuteCommand("/cpose");

                    PluginLog.LogDebug(
                        "Execute /cpose to get from {currentPoseIndex} to {newPoseIndex} of {currentPoseType:l}.",
                        currentActorPoseIndex,
                        newPoseIndex,
                        currentPoseType
                    );

                    Task.Delay(TimeSpan.FromMilliseconds(50));
                }

                if (i >= currentPoseCount)
                {
                    PluginLog.LogError("Could not change pose of {currentPoseType:l}.", currentPoseType);
                    WritePose(newPoseType, newPoseIndex);
                }
            }
        }

        private unsafe int GetCurrentPose()
        {
            var player = _clientState.LocalPlayer;
            if (player == null)
            {
                return -1;
            }

            var playerPointer = player.Address;

            var ptr = (byte*)playerPointer.ToPointer();
            return *(ptr + Offsets.Character.CPose);
        }

        private unsafe byte GetPose(PoseType poseType)
        {
            var ptr = (byte*)_gameManager.CPoseSettings.ToPointer();
            return ptr[(int)poseType];
        }

        private unsafe void WritePose(PoseType poseType, byte poseIndex)
        {
            var ptr = (byte*)_gameManager.CPoseSettings.ToPointer();
            ptr[(int)poseType] = poseIndex;
        }
    }
}
