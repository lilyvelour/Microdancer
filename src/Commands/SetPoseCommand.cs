using System;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using XivCommon;

namespace Microdancer
{
    public sealed class SetPoseCommand : CommandBase, IDisposable
    {
        private readonly CPoseManager _cPoseManager;
        private readonly XivCommonBase _xiv;

        private bool _disposedValue;

        public SetPoseCommand(CPoseManager cPoseManager) : base()
        {
            _cPoseManager = cPoseManager;
            _xiv = new XivCommonBase((Hooks)~0);
        }

        [Command("setpose", HelpMessage = "Use with [stand, weapon, sit, groundsit, doze] [#] to set a specific pose.")]
        public void SetCPose(string[] args)
        {
            if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                _xiv.Functions.Chat.SendMessage("/cpose");
                return;
            }

            var poseType = args.Length < 2 ? "stand" : args[0].ToLowerInvariant();
            var poseTypes = new[] { "stand", "weapon", "sit", "groundsit", "doze" };

            var whichPoseType = Array.IndexOf(poseTypes, poseType);
            if (whichPoseType < 0)
            {
                if (!int.TryParse(args[0], out whichPoseType) || whichPoseType < 0 || whichPoseType > poseTypes.Length)
                {
                    PrintError("setpose", $"Invalid pose type \"{poseType}\"");
                    return;
                }
                poseType = poseTypes[whichPoseType];
            }

            if (!byte.TryParse(args[^1], out var whichPose))
            {
                PrintError("setpose", "Pose must be a valid number");
                return;
            }

            if (whichPose == 0 || whichPose > CPoseManager.NumPoses[whichPoseType])
            {
                PrintError(
                    "setpose",
                    $"Pose {whichPose} for {poseType} does not exist. Only {CPoseManager.NumPoses[whichPoseType]} poses are supported."
                );
                return;
            }

            _cPoseManager.SetPose(whichPoseType, (byte)(whichPose - 1));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                _xiv.Dispose();
            }

            _disposedValue = true;
        }
    }
}
