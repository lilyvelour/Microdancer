using System;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using Dalamud.Logging;

namespace Microdancer
{
    [PluginInterface]
    public class CPoseManager
    {
        private readonly GameManager _gameManager;
        private readonly ClientState _clientState;

        public CPoseManager(GameManager gameManager, ClientState clientState)
        {
            _gameManager = gameManager;
            _clientState = clientState;

            ResetDefaultPoses();
        }

        public const int NumStandingPoses = 7;
        public const int NumWeaponDrawnPoses = 2;
        public const int NumSitPoses = 3;
        public const int NumGroundSitPoses = 4;
        public const int NumDozePoses = 3;

        public static readonly int[] NumPoses =
        {
            NumStandingPoses,
            NumWeaponDrawnPoses,
            NumSitPoses,
            NumGroundSitPoses,
            NumDozePoses,
        };

        public static readonly string[] PoseNames =
        {
            "Standing Pose",
            "Weapon Drawn Pose",
            "Sitting Pose",
            "Sitting on Ground Pose",
            "Dozing Pose",
        };

        private static byte TranslateState(byte state, bool weaponDrawn)
        {
            return state switch
            {
                1 => 3,
                2 => 2,
                3 => 4,
                _ => (byte)(weaponDrawn ? 1 : 0),
            };
        }

        public const byte DefaultPose = byte.MaxValue;
        public const byte UnchangedPose = byte.MaxValue - 1;

        private readonly byte[] _defaultPoses = new byte[5];

        public byte DefaultStandingPose => _defaultPoses[0];

        public byte DefaultWeaponDrawnPose => _defaultPoses[1];

        public byte DefaultSitPose => _defaultPoses[2];

        public byte DefaultGroundSitPose => _defaultPoses[3];

        public byte DefaultDozePose => _defaultPoses[4];

        public byte StandingPose => GetPose(0);

        public byte WeaponDrawnPose => GetPose(1);

        public byte SitPose => GetPose(2);

        public byte GroundSitPose => GetPose(3);

        public byte DozePose => GetPose(4);

        public void SetStandingPose(byte pose) => SetPose(0, pose);

        public void SetWeaponDrawnPose(byte pose) => SetPose(1, pose);

        public void SetSitPose(byte pose) => SetPose(2, pose);

        public void SetGroundSitPose(byte pose) => SetPose(3, pose);

        public void SetDozePose(byte pose) => SetPose(4, pose);

        public bool WeaponDrawn { get; set; } = false;

        private unsafe byte GetSeatingState(IntPtr playerPointer)
        {
            const int seatingStateOffset = 0x19D7;
            var ptr = (byte*)playerPointer.ToPointer();
            return *(ptr + seatingStateOffset);
        }

        private unsafe int GetCPoseActorState(IntPtr playerPointer)
        {
            const int cPoseOffset = 0xC11;
            var ptr = (byte*)playerPointer.ToPointer();
            return *(ptr + cPoseOffset);
        }

        private unsafe byte GetPose(int which)
        {
            var ptr = (byte*)_gameManager.CPoseSettings.ToPointer();
            return ptr[which];
        }

        private unsafe void WritePose(int which, byte pose)
        {
            var ptr = (byte*)_gameManager.CPoseSettings.ToPointer();
            ptr[which] = pose;
        }

        public void SetPose(int which, byte toWhat)
        {
            if (toWhat == UnchangedPose)
                return;

            if (toWhat == DefaultPose)
            {
                toWhat = _defaultPoses[which];
            }
            else if (toWhat >= NumPoses[which])
            {
                PluginLog.LogError(
                    $"Higher pose requested than possible for {PoseNames[which]}: {toWhat} / {NumPoses[which]}."
                );
                return;
            }

            var player = _clientState.LocalPlayer;
            var playerPointer = player?.Address ?? IntPtr.Zero;

            if (playerPointer == IntPtr.Zero)
                return;

            var currentState = GetSeatingState(playerPointer);
            currentState = TranslateState(currentState, WeaponDrawn);
            var pose = GetPose(which);
            if (currentState == which)
            {
                if (toWhat == GetCPoseActorState(playerPointer))
                {
                    if (pose != toWhat)
                    {
                        WritePose(which, toWhat);
                        PluginLog.LogDebug(
                            "Overwrote {OldPose} with {NewPose} for {WhichPose:l}, currently in {CurrentState:l}.",
                            pose,
                            toWhat,
                            PoseNames[which],
                            PoseNames[currentState]
                        );
                    }
                }
                else
                {
                    Task.Run(
                        () =>
                        {
                            var i = 0;
                            do
                            {
                                PluginLog.LogDebug(
                                    "Execute /setpose to get from {OldPose} to {NewPose} of {CurrentState:l}.",
                                    pose,
                                    toWhat,
                                    PoseNames[currentState]
                                );
                                _gameManager.ExecuteCommand("/cpose");
                                Task.Delay(TimeSpan.FromMilliseconds(50));
                            } while (toWhat != GetCPoseActorState(playerPointer) && i++ < 8);
                            if (i > 8)
                            {
                                PluginLog.LogError(
                                    "Could not change pose of {CurrentState:l}.",
                                    PoseNames[GetCPoseActorState(playerPointer)]
                                );
                            }
                        }
                    );
                }
            }
            else if (pose != toWhat)
            {
                WritePose(which, toWhat);
                PluginLog.LogDebug(
                    "Overwrote {OldPose} with {NewPose} for {WhichPose:l}, currently in {CurrentState:l}.",
                    pose,
                    toWhat,
                    PoseNames[which],
                    PoseNames[currentState]
                );
            }
        }

        public void SetPoses(byte standing, byte weaponDrawn, byte sitting, byte groundSitting, byte dozing)
        {
            SetPose(0, standing);
            SetPose(1, weaponDrawn);
            SetPose(2, sitting);
            SetPose(3, groundSitting);
            SetPose(4, dozing);
        }

        public void ResetDefaultPoses()
        {
            _defaultPoses[0] = GetPose(0);
            _defaultPoses[1] = GetPose(1);
            _defaultPoses[2] = GetPose(2);
            _defaultPoses[3] = GetPose(3);
            _defaultPoses[4] = GetPose(4);
        }
    }
}
