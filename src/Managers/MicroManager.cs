using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.IoC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;
using XivCommon;

namespace Microdancer
{
    [PluginInterface]
    public class MicroManager : IDisposable
    {
        private bool _disposedValue;
        private bool _ready;
        private readonly Framework _framework;
        private readonly ClientState _clientState;
        private readonly Condition _condition;
        private readonly XivCommonBase _xiv;

        private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();
        private readonly ConcurrentDictionary<Guid, MicroInfo> _running = new();
        private readonly ConcurrentDictionary<Guid, bool> _cancelled = new();
        private readonly ConcurrentDictionary<Guid, bool> _paused = new();

        private bool? _autoBusy;

        public MicroManager(Framework framework, ClientState clientState, Condition condition)
        {
            _framework = framework;
            _clientState = clientState;
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

        public Guid RunMicro(Micro micro, string? region = null)
        {
            if (!_ready)
            {
                return Guid.Empty;
            }

            var microInfo = new MicroInfo(micro, region);
            _running.TryAdd(microInfo.Id, microInfo);
            microInfo.StartTime = DateTime.Now;

            Task.Run(() => ExecuteMicro(microInfo));

            return microInfo.Id;
        }

        public IEnumerable<MicroInfo> GetQueue()
        {
            return _running.Values;
        }

        public IEnumerable<MicroInfo> Find(Guid id)
        {
            if (_running.TryGetValue(id, out var value))
            {
                return new[] { value };
            }

            return _running.Values.Where(mi => mi.Micro.Id == id);
        }

        public bool IsRunning(Guid id)
        {
            return Find(id).Any();
        }

        public bool IsRunning(MicroInfo info)
        {
            return _running.ContainsKey(info.Id);
        }

        public bool IsPaused(Guid id)
        {
            foreach (var mi in Find(id))
            {
                if (IsPaused(mi))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPaused(MicroInfo info)
        {
            _paused.TryGetValue(info.Id, out var paused);
            return paused;
        }

        public bool IsCancelled(Guid id)
        {
            foreach (var mi in Find(id))
            {
                if (IsCancelled(mi))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsCancelled(MicroInfo info)
        {
            _cancelled.TryGetValue(info.Id, out var cancelled);
            return cancelled;
        }

        public void Pause(Guid id)
        {
            foreach (var mi in Find(id))
            {
                Pause(mi);
            }
        }

        public void Pause(MicroInfo info)
        {
            _paused.TryAdd(info.Id, true);
        }

        public void Resume(Guid id)
        {
            foreach (var mi in Find(id))
            {
                Resume(mi);
            }
        }

        public void Resume(MicroInfo info)
        {
            _paused.TryRemove(info.Id, out _);
        }

        public void Cancel(MicroInfo info)
        {
            _cancelled.TryAdd(info.Id, true);
        }

        public void Cancel(Guid id)
        {
            foreach (var mi in Find(id))
            {
                Cancel(mi);
            }
        }

        public void CancelAll()
        {
            foreach (var running in _running.Keys)
            {
                Cancel(running);
            }
        }

        private void Login(object? sender, EventArgs args)
        {
            _ready = true;
        }

        private void Logout(object? sender, EventArgs args)
        {
            _ready = false;

            foreach (var id in _running.Keys)
            {
                Cancel(id);
            }
        }

        private void Update(Framework _)
        {
            // get a message to send, but discard it if we're not ready
            if (!_channel.Reader.TryRead(out var command) || !_ready)
            {
                return;
            }

            // send the message as if it were entered in the chat box
            _xiv.Functions.Chat.SendMessage(command);
        }

        private async Task ExecuteMicro(MicroInfo microInfo)
        {
            // the default wait
            TimeSpan? defaultWait = null;

            // auto-countdown
            string? autoCountdown = null;

            // keep track of the line we're at in the Micro
            var i = 0;

            while (i < microInfo.Commands.Length)
            {
                // cancel if requested
                if (_cancelled.TryRemove(microInfo.Id, out var cancel) && cancel)
                {
                    microInfo.WasCancelled = true;

                    DisableBusy();

                    break;
                }

                // wait instead of executing if paused
                if (_paused.TryGetValue(microInfo.Id, out var paused) && paused)
                {
                    await Task.Delay(defaultWait ?? TimeSpan.FromMilliseconds(10));
                    continue;
                }
                // force pause if in combat to prevent bad automation
                else if (_condition[ConditionFlag.InCombat])
                {
                    Pause(microInfo.Id);
                    continue;
                }

                // get the line of the command
                var command = microInfo.Commands[i];

                microInfo.CurrentCommand = command;
                microInfo.CurrentCommand.StartTime = DateTime.Now;

                // handle entering a new region
                if (command.Region != null && command.Region.StartTime == null)
                {
                    command.Region.StartTime = microInfo.CurrentCommand.StartTime;

                    if (autoCountdown != null)
                    {
                        DispatchAutoCountdown(microInfo, command.Region, autoCountdown, i);
                    }
                }

                // go back to the beginning if the command is loop
                if (command.Text == "/loop")
                {
                    if (_autoBusy == true)
                    {
                        _autoBusy = false;
                    }

                    i = 0;
                    microInfo.CurrentCommand = null;
                    microInfo.StartTime = DateTime.Now;

                    continue;
                }
                // set auto-busy
                else if (command.Text.StartsWith("/autobusy"))
                {
                    await SetBusy();

                    ++i;
                    continue;
                }
                // set auto-countdown
                else if (command.Text.StartsWith("/autocountdown") || command.Text.StartsWith("/autocd"))
                {
                    autoCountdown = SetAutoCountdown(command.Text);

                    ++i;
                    continue;
                }
                // send the command to the channel (ignore /wait)
                else if (command.Text != "/wait")
                {
                    await _channel.Writer.WriteAsync(command.Text);
                }

                await Task.Delay(command.WaitTime);

                microInfo.CurrentCommand = null;

                ++i;
            }

            _running.TryRemove(microInfo.Id, out _);

            DisableBusy();
        }

        private void DispatchAutoCountdown(MicroInfo microInfo, MicroRegion region, string autoCountdown, int i)
        {
            // Dispatch countdown task if we have any additional regions (unless we're in a named region)
            var isNamedRegion = region.IsNamedRegion;

            if (isNamedRegion)
            {
                return;
            }

            var regionIsLongEnough = region.WaitTime > TimeSpan.FromSeconds(9);

            if (!regionIsLongEnough)
            {
                return;
            }

            var hasNextRegion = microInfo.Commands[i..].Any(
                c => c.Region != region && c.Region?.IsNamedRegion == false
            );

            if (!hasNextRegion)
            {
                return;
            }

            var cdTime = TimeSpan.FromSeconds(autoCountdown == "start" ? 4.98 : 5.48);
            var delay = region.WaitTime - cdTime;
            var end = region.StartTime + region.WaitTime;

            Task.Run(
                    async () =>
                    {
                        await Task.Delay(delay);

                        if (microInfo.WasCancelled)
                            return;

                        if (end - DateTime.Now <= cdTime)
                        {
                            await _channel.Writer.WriteAsync("/cd 5");
                        }
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
                else
                {
                    autoCountdown = "pulse";
                }
            }

            return autoCountdown;
        }

        private async Task SetBusy()
        {
            await _channel.Writer.WriteAsync("/busy on");
            _autoBusy = true;
        }

        private void DisableBusy()
        {
            if (_autoBusy == true)
            {
                _autoBusy = false;

                Task.Run(
                        async () =>
                        {
                            await Task.Delay(TimeSpan.FromSeconds(2.5));

                            if (_autoBusy == false)
                            {
                                _autoBusy = null;
                                await _channel.Writer.WriteAsync("/busy off");
                            }
                        }
                    )
                    .ConfigureAwait(false);
            }
        }
    }
}
