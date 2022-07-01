using System;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;

namespace Microdancer
{
    public sealed class SetPoseCommand : CommandBase
    {
        private readonly CPoseManager _cPoseManager;

        public SetPoseCommand(Service.Locator serviceLocator) : base(serviceLocator)
        {
            _cPoseManager = serviceLocator.Get<CPoseManager>();
        }

        [Command(
            "setpose",
            HelpMessage = "Use with [stand, weapon, sit, groundsit, doze, parasol] [#] to set a specific pose."
        )]
        public void SetCPose(string[] args)
        {
            var poseType =
                args.Length < 2
                    ? _cPoseManager.GetCurrentPoseType()
                    : _cPoseManager.GetPoseTypeFromName(args[0].ToLowerInvariant());

            if (poseType == CPoseManager.PoseType.Invalid)
            {
                PrintError($"Invalid pose type specified");
                return;
            }

            if (!uint.TryParse(args[^1], out var pose))
            {
                PrintError("Pose must be a valid positive number");
                return;
            }

            var poseCount = _cPoseManager.GetPoseCount(poseType);

            if (pose > poseCount)
            {
                PrintError($"Pose {pose} for {poseType} does not exist. Only {poseCount} poses are supported.");
                return;
            }

            _cPoseManager.SetPose(poseType, (byte)(pose - 1));
        }
    }
}
