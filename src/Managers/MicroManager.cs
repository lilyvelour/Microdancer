﻿using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Plugin;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microdancer
{
    public class MicroManager : IDisposable
    {
        public const int FAST_FRAME_TIME = 33;
        public const int DEFAULT_FRAME_TIME = 33;
        public int _frameTime = DEFAULT_FRAME_TIME;

        private bool _disposedValue;
        private bool _ready;
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly ClientState _clientState;
        private readonly ChatGui _chatGui;
        private readonly GameManager _gameManager;

        private bool? _autoBusy;
        private bool? _autoMare;

        public MicroInfo? Current { get; private set; }

        public MicroManager(
            DalamudPluginInterface pluginInterface,
            ClientState clientState,
            ChatGui chatGui,
            Service.Locator serviceLocator
        )
        {
            _pluginInterface = pluginInterface;
            _clientState = clientState;
            _chatGui = chatGui;
            _gameManager = serviceLocator.Get<GameManager>();

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
            if (_gameManager.IsInCombatOrPvP)
            {
                _chatGui.PrintError("Micros cannot be used in combat or PvP.");
                return;
            }

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

            if (_gameManager.IsInCombatOrPvP)
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

            // Flags
            if (microInfo.FastFrames)
            {
                _frameTime = FAST_FRAME_TIME;
            }

            // Keep track of the line we're at in the Micro
            var i = 0;

            // Set current time to zero
            microInfo.Start();

            var autoPing = TimeSpan.Zero;
            var delay = TimeSpan.Zero;

            while (i < microInfo.Commands.Length)
            {
                if (GetPlaybackState(microInfo, out var shouldBreakLoop) == PlaybackState.Paused)
                {
                    await Task.Delay(_frameTime);

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

                    if (_autoMare == true)
                    {
                        _autoMare = false;
                    }

                    i = 0;
                    microInfo.Loop();
                    continue;
                }
                // Set auto-busy
                else if (command.Text.StartsWith("/autobusy") || command.Text.StartsWith("/autobussy"))
                {
                    _autoBusy = true;
                    await _gameManager.ExecuteCommand("/busy on");

                    if (command.Text.StartsWith("/autobussy"))
                    {
                        for (var s = 1; s <= 12; ++s)
                        {
                            // This is Kibby's fault
                            await _gameManager.ExecuteCommand($"/echo Bussy Gang Bussy Gang Bussy Gang <se.{s}>");
                        }
                    }

                    ++i;
                    continue;
                }
                // Set auto-mare
                else if (command.Text.StartsWith("/automare"))
                {
                    _autoMare = true;
                    await _gameManager.ExecuteCommand("/mare toggle off");

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
                else if (command.Text.StartsWith("/acancel")) // Animation cancel
                {
                    await _gameManager.ExecuteCommand("/gaction \"Glamour Plate\"");
                    await _gameManager.ExecuteCommand("/gaction \"Glamour Plate\"");
                }
                else if (command.Text.StartsWith("/pad ") || command.Text.StartsWith("/padding "))
                {
                    if (command.Text.Length > 5)
                    {
                        var padCommand = command.Text[(command.Text.StartsWith("/padding ") ? 9 : 5)..];
                        var padCount = 40;
                        var split = padCommand.Split();
                        if (split.Length > 1)
                        {
                            if (int.TryParse(split[0], out padCount))
                            {
                                padCommand = padCommand[(padCount.ToString().Length + 1)..];
                            }
                            else
                            {
                                padCount = 40;
                            }
                        }

                        padCount = Math.Clamp(padCount, 1, 100);

                        _ = Task.Run(
                            async () =>
                            {
                                for (var i = 0; i < padCount; ++i)
                                {
                                    await _gameManager.ExecuteCommand($"/{padCommand}");
                                    Thread.Sleep(10);
                                }
                            }
                        );
                    }
                }
                // Send the command to the channel (ignore /microcancel)
                else if (command.Text != "/microcancel")
                {
                    await _gameManager.ExecuteCommand(command.Text);
                }

                var waitTime = command.WaitTime - autoPing;
                delay += waitTime - command.CurrentTime;
                while (delay > TimeSpan.Zero)
                {
                    autoPing = TimeSpan.Zero;

                    var t = command.CurrentTime;
                    await Task.Delay(Math.Min((int)delay.TotalMilliseconds, _frameTime));
                    var dt = command.CurrentTime - t;

                    // Micro is currently paused
                    if (GetPlaybackState(microInfo, out shouldBreakLoop) == PlaybackState.Paused)
                    {
                        continue;
                    }

                    if (shouldBreakLoop)
                    {
                        break;
                    }

                    delay -= dt;
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
                _frameTime = DEFAULT_FRAME_TIME;
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
                            await _gameManager.ExecuteCommand("/busy off");
                        }
                    }
                );
            }

            if (_autoMare == true)
            {
                _autoMare = false;

                await Task.Run(
                    async () =>
                    {
                        await Task.Delay(TimeSpan.FromMinutes(10));

                        if (_autoMare == false)
                        {
                            _autoMare = null;
                            await _gameManager.ExecuteCommand("/mare toggle on");
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

            var cd = autoCountdown == "start" ? 5.0 : 6.0;
            const double reactionTimeMagicNumber = 0.02; // 20 ms
            var cdTime = TimeSpan.FromSeconds(cd - reactionTimeMagicNumber);
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

                        await _gameManager.ExecuteCommand("/cd 5");
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
