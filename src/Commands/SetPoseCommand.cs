using System;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using XivCommon;

namespace Microdancer
{
    public sealed class SetPoseCommand : CommandBase, IDisposable
    {
        private readonly ChatGui _chatGui;
        private readonly CPoseManager _cPoseManager;
        private readonly XivCommonBase _xiv;

        private bool _disposedValue;

        public SetPoseCommand(
            CommandManager commandManager,
            Configuration configuration,
            ChatGui chatGui,
            CPoseManager cPoseManager) : base(commandManager, configuration)
        {
            _chatGui = chatGui;
            _cPoseManager = cPoseManager;
            _xiv = new XivCommonBase((Hooks)~0);
        }

        [Command("setpose", HelpMessage = "Use with [stand, weapon, sit, groundsit, doze] [#] to set a specific pose.")]
        public void SetCPose(params string[] args)
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
                    _chatGui.PrintError($"Invalid pose type \"{poseType}\"");
                    return;
                }
                poseType = poseTypes[whichPoseType];
            }

            if (!byte.TryParse(args[^1], out var whichPose))
            {
                _chatGui.PrintError($"Pose must be a valid number");
                return;
            }

            if (whichPose == 0 || whichPose > CPoseManager.NumPoses[whichPoseType])
            {
                _chatGui.PrintError(
                    $"Pose {whichPose} for {poseType} does not exist. Only {CPoseManager.NumPoses[whichPoseType]} poses are supported.");
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