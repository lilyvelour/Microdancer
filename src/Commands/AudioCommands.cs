using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Game;

namespace Microdancer
{
    public sealed class AudioCommands : CommandBase
    {
        private class VolumeTriggerInfo
        {
            public VolumeComparison Comparison { get; set; }
            public float PeakValue_dB { get; set; }

            public string Command { get; set; }
        }

        private enum VolumeComparison
        {
            LessThan = -1,
            GreaterThan = 1
        }

        private readonly AudioManager _audioManager;
        private readonly GameManager _gameManager;
        private readonly Framework _framework;
        private readonly TimeSpan _updateInterval;

        private readonly List<VolumeTriggerInfo> _volumeTriggers = new();

        private DateTime _lastUpdate;
        private float _lastPeakValue_dB = float.NegativeInfinity;

        public AudioCommands(Service.Locator serviceLocator) : base(serviceLocator)
        {
            _audioManager = serviceLocator.Get<AudioManager>();
            _gameManager = serviceLocator.Get<GameManager>();

            _framework = serviceLocator.Get<Framework>();
            _framework.Update += OnUpdate;

            _updateInterval = TimeSpan.FromMilliseconds(10);
        }

        private async void OnUpdate(Framework framework)
        {
            var now = DateTime.Now;
            if (now - _lastUpdate >= _updateInterval)
            {
                var peakValue_dB = _audioManager.LinearToDecibel(_audioManager.SmoothedPeakValue);

                foreach (var trigger in _volumeTriggers)
                {
                    var matchFound = false;

                    switch (trigger.Comparison)
                    {
                        case VolumeComparison.LessThan:
                            if (peakValue_dB < trigger.PeakValue_dB && _lastPeakValue_dB >= trigger.PeakValue_dB)
                            {
                                await _gameManager.ExecuteCommand(trigger.Command);
                                matchFound = true;
                            }
                            break;
                        case VolumeComparison.GreaterThan:
                            if (peakValue_dB > trigger.PeakValue_dB && _lastPeakValue_dB <= trigger.PeakValue_dB)
                            {
                                await _gameManager.ExecuteCommand(trigger.Command);
                                matchFound = true;
                            }
                            break;
                        default:
                            break; // Invalid
                    }

                    if (matchFound)
                    {
                        break;
                    }
                }
                _lastUpdate = now;
                _lastPeakValue_dB = peakValue_dB;
            }
        }

        [Command("captureaudio", "recordaudio", HelpMessage = "Enable or disable audio capture.")]
        public void RecordAudio(bool? toggle = null)
        {
            switch (toggle)
            {
                case true:
                    _audioManager.StartRecording();
                    break;
                case false:
                    _volumeTriggers.Clear();
                    _audioManager.StopRecording();
                    break;
                default:
                    _audioManager.ToggleRecording();
                    break;
            }
        }

        [Command("volumetrigger", HelpMessage = "", Raw = true)]
        public void VolumeTrigger(string args)
        {
            var split = args.Split();

            if (split.Length == 1 && split[0] == "clear")
            {
                Print("Volume triggers cleared.");
                return;
            }

            if (split.Length < 3)
            {
                PrintError("Invalid format. Expected: /audiotrigger [dB] [>, <] [command]");
                return;
            }

            var dBStr = split[0];
            var comparison = split[1];
            var cmd = string.Join(' ', split[2..]);

            if (!float.TryParse(dBStr, out var dB))
            {
                PrintError("Invalid decibel value. Numerical value expected.");
            }

            _volumeTriggers.Add(
                new VolumeTriggerInfo
                {
                    Comparison = comparison == "<" ? VolumeComparison.LessThan : VolumeComparison.GreaterThan,
                    PeakValue_dB = dB,
                    Command = cmd
                }
            );
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _framework.Update -= OnUpdate;
            }
        }
    }
}
