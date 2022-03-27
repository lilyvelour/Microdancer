using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using XivCommon;

namespace Microdancer
{
    [PluginInterface]
    public class MicroManager : IDisposable
    {
        public const int FRAME_TIME = 33;

        private bool _disposedValue;
        private bool _ready;
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly Framework _framework;
        private readonly ClientState _clientState;
        private readonly GameManager _gameManager;
        private readonly Condition _condition;
        private readonly XivCommonBase _xiv;
        private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();

        private bool? _autoBusy;

        public MicroInfo? Current { get; private set; }

        public MicroManager(
            DalamudPluginInterface pluginInterface,
            Framework framework,
            ClientState clientState,
            GameManager gameManager,
            Condition condition
        )
        {
            _pluginInterface = pluginInterface;
            _framework = framework;
            _clientState = clientState;
            _gameManager = gameManager;
            _condition = condition;
            _xiv = new XivCommonBase((Hooks)~0);

            _framework.Update += Update;
            _clientState.Login += Login;
            _clientState.Logout += Logout;

            _ready = _clientState.LocalPlayer != null;
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
                _clientState.Login -= Login;
                _clientState.Logout -= Logout;
            }

            _disposedValue = true;
        }

        public void StartMicro(Micro micro, string? region = null)
        {
            if (!_ready)
            {
                return;
            }

            StartMicro(new MicroInfo(micro, region));
        }

        public void StartMicro(Micro micro, int lineNumber)
        {
            if (!_ready)
            {
                return;
            }

            StartMicro(new MicroInfo(micro, lineNumber));
        }

        public void StartMicro(MicroInfo microInfo)
        {
            if (!_ready)
            {
                return;
            }

            Current?.Stop();

            Current = microInfo;

            Task.Run(() => ExecuteMicro(Current));
        }

        private void Login(object? sender, EventArgs args)
        {
            _ready = true;
        }

        private void Logout(object? sender, EventArgs args)
        {
            _ready = false;

            Current?.Stop();
            Current = null;
        }

        private void Update(Framework _)
        {
            while (true)
            {
                if (!_channel.Reader.TryRead(out var command) || !_ready)
                {
                    return;
                }

                _gameManager.ActionCommandRequestType = 0;
                _xiv.Functions.Chat.SendMessage(command);
                _gameManager.ActionCommandRequestType = 2;
            }
        }

        public PlaybackState PlaybackState
        {
            get
            {
                if (Current == null)
                {
                    return PlaybackState.Stopped;
                }

                bool _;
                return GetPlaybackState(Current, out _);
            }
        }

        private PlaybackState GetPlaybackState(MicroInfo microInfo, out bool shouldBreakLoop)
        {
            shouldBreakLoop = microInfo != Current;

            if (_condition[ConditionFlag.InCombat])
            {
                microInfo.Stop();
                shouldBreakLoop = true;
                return PlaybackState.Stopped;
            }

            if (microInfo.IsPaused)
            {
                return PlaybackState.Paused;
            }

            if (!microInfo.IsPlaying)
            {
                shouldBreakLoop = true;
                return PlaybackState.Stopped;
            }

            return PlaybackState.Playing;
        }

        private async Task ExecuteMicro(MicroInfo microInfo)
        {
            // Auto-countdown
            string? autoCountdown = null;

            // Keep track of the line we're at in the Micro
            var i = 0;

            // Set current time to zero
            microInfo.Start();

            var autoPing = TimeSpan.Zero;

            while (i < microInfo.Commands.Length)
            {
                if (GetPlaybackState(microInfo, out var shouldBreakLoop) == PlaybackState.Paused)
                {
                    await Task.Delay(FRAME_TIME);

                    if (shouldBreakLoop)
                    {
                        break;
                    }

                    continue;
                }

                if (shouldBreakLoop)
                {
                    break;
                }

                // Get the line of the command
                var command = microInfo.Commands[i];
                MicroCommand? lastCommand = null;
                if (i > 1)
                {
                    lastCommand = microInfo.Commands[i - 1];
                }

                microInfo.CurrentCommand = command;

                // Handle entering a new region
                if (command.Region != lastCommand?.Region)
                {
                    if (autoCountdown == "off")
                    {
                        autoCountdown = null;
                    }
                    else if (autoCountdown != null)
                    {
                        DispatchAutoCountdown(microInfo, command.Region, autoCountdown, i);
                    }
                }

                // Go back to the beginning if the command is a loop
                if (command.Text == "/loop" && microInfo.Commands.Length > 1)
                {
                    if (_pluginInterface.Configuration().IgnoreLooping)
                    {
                        ++i;
                        continue;
                    }

                    if (_autoBusy == true)
                    {
                        _autoBusy = false;
                    }

                    i = 0;
                    microInfo.Loop();
                    continue;
                }
                // Set auto-busy
                else if (command.Text.StartsWith("/autobusy"))
                {
                    _autoBusy = true;
                    await _channel.Writer.WriteAsync("/busy on");

                    ++i;
                    continue;
                }
                // Set auto-countdown
                else if (command.Text.StartsWith("/autocountdown") || command.Text.StartsWith("/autocd"))
                {
                    autoCountdown = SetAutoCountdown(command.Text);

                    ++i;
                    continue;
                }
                // Set auto-countdown
                else if (command.Text.StartsWith("/autoping") || command.Text.StartsWith("/autoping"))
                {
                    autoPing = TimeSpan.FromMilliseconds(57); // TODO: Not hardcoded ping!

                    ++i;
                    continue;
                }
                // Send the command to the channel (ignore /wait and /microcancel)
                else if (command.Text != "/wait" && command.Text != "/microcancel")
                {
                    await _channel.Writer.WriteAsync(command.Text);
                }

                var waitTime = command.WaitTime - autoPing;
                var delay = waitTime - command.CurrentTime;
                var resetAutoPing = false;

                while (delay > TimeSpan.Zero)
                {
                    resetAutoPing = true;

                    await Task.Delay(Math.Min((int)delay.TotalMilliseconds, FRAME_TIME));

                    // Micro is currently paused
                    if (GetPlaybackState(microInfo, out shouldBreakLoop) == PlaybackState.Paused)
                    {
                        continue;
                    }

                    if (shouldBreakLoop)
                    {
                        break;
                    }

                    delay = waitTime - command.CurrentTime;
                }

                if (resetAutoPing)
                {
                    autoPing = TimeSpan.Zero;
                }

                // Micro is currently paused
                if (GetPlaybackState(microInfo, out shouldBreakLoop) == PlaybackState.Paused)
                {
                    continue;
                }

                if (shouldBreakLoop)
                {
                    break;
                }

                ++i;
            }

            microInfo.Stop();
            microInfo.CurrentCommand = null;

            if (Current == microInfo)
            {
                Current = null;
            }

            if (_autoBusy == true)
            {
                _autoBusy = false;

                await Task.Run(
                    async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2.5));

                        if (_autoBusy == false)
                        {
                            _autoBusy = null;
                            await _channel.Writer.WriteAsync("/busy off");
                        }
                    }
                );
            }
        }

        private void DispatchAutoCountdown(MicroInfo microInfo, MicroRegion region, string autoCountdown, int i)
        {
            // Dispatch countdown task if we have any additional regions (unless we're in a named or default region)
            var isNamedOrDefaultRegion = region.IsNamedRegion || region.IsDefaultRegion;

            if (isNamedOrDefaultRegion)
            {
                return;
            }

            var regionIsLongEnough = region.WaitTime > TimeSpan.FromSeconds(9);

            if (!regionIsLongEnough)
            {
                return;
            }

            var hasNextRegion = microInfo.Commands[i..].Any(
                c => c.Region != region && !c.Region.IsNamedRegion && !c.Region.IsDefaultRegion
            );

            if (!hasNextRegion)
            {
                return;
            }

            var cdTime = TimeSpan.FromSeconds(autoCountdown == "start" ? 4.98 : 5.48);
            var delay = region.WaitTime - cdTime;

            Task.Run(
                    async () =>
                    {
                        await Task.Delay(delay);

                        if (
                            _pluginInterface.Configuration().IgnoreAutoCountdown
                            || Current != microInfo
                            || PlaybackState != PlaybackState.Playing
                        )
                        {
                            return;
                        }

                        await _channel.Writer.WriteAsync("/cd 5");
                    }
                )
                .ConfigureAwait(false);
        }

        private static string SetAutoCountdown(string command)
        {
            string? autoCountdown;
            if (command == "/autocountdown" || command == "/autocd")
            {
                // default to pulse
                autoCountdown = "pulse";
            }
            else
            {
                var autocd = command.Split(' ')[^1].Trim();
                if (string.Equals(autocd, "start", StringComparison.InvariantCultureIgnoreCase))
                {
                    autoCountdown = "start";
                }
                else if (string.Equals(autocd, "off", StringComparison.InvariantCultureIgnoreCase))
                {
                    autoCountdown = "off";
                }
                else
                {
                    autoCountdown = "pulse";
                }
            }

            return autoCountdown;
        }
    }
}
