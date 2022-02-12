using System;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using XivCommon;

namespace Microdancer
{
    public sealed class SetCPoseCommand : CommandBase, IDisposable
    {
        private readonly ChatGui _chatGui;
        private readonly CPoseManager _cPoseManager;
        private readonly XivCommonBase _xiv;

        private bool _disposedValue;

        public SetCPoseCommand(
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
            if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
            {
                _xiv.Functions.Chat.SendMessage("/cpose");
                return;
            }

            string[] poseTypes =
            {
                "Stand",
                "Weapon",
                "Sit",
                "GroundSit",
                "Doze",
            };

            var poseType = args.Length < 2 ? "stand" : args[0].ToLowerInvariant();

            int whichPoseType;
            switch (poseType)
            {
                case "stand":
                    whichPoseType = 0;
                    break;
                case "weapon":
                    whichPoseType = 1;
                    break;
                case "sit":
                    whichPoseType = 2;
                    break;
                case "groundsit":
                    whichPoseType = 3;
                    break;
                case "doze":
                    whichPoseType = 4;
                    break;
                default:
                    if (!int.TryParse(args[0], out whichPoseType) || whichPoseType < 0 || whichPoseType > 4)
                    {
                        _chatGui.PrintError($"Invalid pose type \"{poseType}\"");
                        return;
                    }
                    break;
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