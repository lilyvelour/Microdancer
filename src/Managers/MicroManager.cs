using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.IoC;
using System;
using System.Collections.Concurrent;
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
        private static readonly Regex Wait =
            new(@"<wait\.(\d+(?:\.\d+)?)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Channel<string> _commands = Channel.CreateUnbounded<string>();
        public ConcurrentDictionary<Guid, MicroInfo> Running { get; } = new();
        private readonly ConcurrentDictionary<Guid, bool> _cancelled = new();
        private readonly ConcurrentDictionary<Guid, bool> _paused = new();

        public MicroManager(
            Framework framework,
            ClientState clientState,
            Condition condition
        )
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

        public Guid SpawnMicro(Micro micro, string? region = null)
        {
            if (!_ready)
            {
                return Guid.Empty;
            }

            var commands = ExtractCommands(micro);
            var id = Guid.NewGuid();
            if (commands.Length == 0)
            {
                // pretend we spawned a task, but actually don't
                return id;
            }
            var microInfo = new MicroInfo(micro)
            {
                CommandsLength = commands.Length
            };

            Running.TryAdd(id, microInfo);
            Task.Run(async () =>
            {
                // the default wait
                TimeSpan? defWait = null;
                // auto-countdown
                string? autoCountdown = null;
                // keep track of the line we're at in the Micro
                var i = 0;
                var shouldExecute = string.IsNullOrWhiteSpace(region);
                var isCancelled = false;
                do
                {
                    // cancel if requested
                    if (_cancelled.TryRemove(id, out var cancel) && cancel)
                    {
                        isCancelled = true;
                        break;
                    }

                    // wait a second instead of executing if paused
                    if (_paused.TryGetValue(id, out var paused) && paused)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        continue;
                    }
                    // force pause if in combat to prevent bad automation
                    else if (_condition[ConditionFlag.InCombat])
                    {
                        PauseMicro(id);
                        continue;
                    }

                    // get the line of the command
                    var command = commands[i];
                    var trimmedCommand = command.Trim();

                    // go back to the beginning if the command is loop
                    if (trimmedCommand == "/loop")
                    {
                        i = 0;
                        microInfo.CurrentRegion = null;
                        defWait = null;
                        continue;
                    }

                    // set default wait
                    if (trimmedCommand.StartsWith("/defaultwait"))
                    {
                        if (trimmedCommand == "/defaultwait")
                        {
                            defWait = null;
                        }
                        else
                        {
                            var defWaitStr = trimmedCommand.Split(' ')[^1].Trim();
                            if (double.TryParse(defWaitStr, out var waitTime))
                            {
                                defWait = TimeSpan.FromSeconds(waitTime);
                            }
                        }

                        i += 1;
                        continue;
                    }

                    // set auto-countdown
                    if (trimmedCommand.StartsWith("/autocountdown") || trimmedCommand.StartsWith("/autocd"))
                    {
                        if (trimmedCommand == "/autocountdown" || trimmedCommand == "/autocd")
                        {
                            autoCountdown = "pulse";
                        }
                        else
                        {
                            var autocd = command.Split(' ')[^1].Trim();
                            if (autocd.ToLowerInvariant() == "start")
                            {
                                autoCountdown = "start";
                            }
                            else
                            {
                                autoCountdown = "pulse";
                            }
                        }

                        i += 1;
                        continue;
                    }

                    // find the amount to wait
                    var wait = ExtractWait(ref command) ?? defWait ?? TimeSpan.FromMilliseconds(10);

                    // handle regions
                    if (trimmedCommand.StartsWith("#region "))
                    {
                        var currentRegion = trimmedCommand[8..];

                        if (currentRegion == region)
                        {
                            shouldExecute = true;
                        }

                        if (region == null || shouldExecute)
                        {
                            microInfo.CurrentRegion = currentRegion;

                            var regionWait = wait;

                            // Get wait time for the whole region
                            var j = Math.Min(i + 1, commands.Length - 1);
                            for(; j < commands.Length; j++)
                            {
                                var cmd = commands[j].Trim();
                                if (cmd.StartsWith("#endregion"))
                                {
                                    break;
                                }
                                regionWait += ExtractWait(ref cmd) ?? defWait ?? TimeSpan.FromMilliseconds(10);
                            }

                            microInfo.CurrentRegionWait = regionWait;
                            microInfo.CurrentRegionStartTime = DateTime.Now;

                            // Dispatch countdown task if we have any additional regions
                            if (autoCountdown != null && regionWait > TimeSpan.FromSeconds(9))
                            {
                                if (commands[j..].Any(c => c.Trim().StartsWith("#region ")))
                                {
                                    var cdTime = TimeSpan.FromSeconds(autoCountdown == "start" ? 4.98 : 5.48);
                                    var delay = regionWait - cdTime;
                                    var end = microInfo.CurrentRegionStartTime + microInfo.CurrentRegionWait;

                                    #pragma warning disable 4014
                                    Task.Run(async () =>
                                    {
                                        await Task.Delay(delay);

                                        if (isCancelled)
                                            return;

                                        if (end - DateTime.Now <= cdTime)
                                        {
                                            await _commands.Writer.WriteAsync($"/cd 5");
                                        }
                                    }).ConfigureAwait(false);
                                    #pragma warning restore 4014
                                }
                            }
                        }

                        i++;
                        continue;
                    }

                    if (trimmedCommand.StartsWith("#endregion"))
                    {
                        if (microInfo.CurrentRegion != null && shouldExecute)
                        {
                            microInfo.CurrentRegion = null;
                            microInfo.CurrentRegionWait = null;
                            microInfo.CurrentCommandStartTime = null;
                        }

                        if (!string.IsNullOrWhiteSpace(region) && shouldExecute)
                        {
                            shouldExecute = false;
                            microInfo.CurrentRegion = null;
                            break;
                        }

                        i++;
                        continue;
                    }

                    if (!shouldExecute)
                    {
                        i++;
                        continue;
                    }

                    microInfo.CurrentCommand = command;
                    microInfo.CurrentCommandIndex = i;
                    microInfo.CurrentCommandWait = wait;

                    // send the command to the channel
                    microInfo.CurrentCommandStartTime = DateTime.Now;
                    await _commands.Writer.WriteAsync(command);

                    await Task.Delay(wait);

                    microInfo.CurrentCommand = string.Empty;
                    microInfo.CurrentCommandStartTime = null;
                    microInfo.CurrentCommandWait = null;

                    // increment to next line
                    i += 1;
                } while (i < commands.Length);

                Running.TryRemove(id, out _);
            });
            return id;
        }

        public bool IsRunning(Guid id)
        {
            return Running.ContainsKey(id);
        }

        public void CancelMicro(Guid id)
        {
            if (!IsRunning(id))
            {
                return;
            }

            _cancelled.TryAdd(id, true);
        }

        public void CancelAllMicros()
        {
            foreach (var running in Running.Keys)
            {
                CancelMicro(running);
            }
        }

        public void PauseMicro(Guid id)
        {
            _paused.TryAdd(id, true);
        }

        public void ResumeMicro(Guid id)
        {
            _paused.TryRemove(id, out _);
        }

        public bool IsPaused(Guid id)
        {
            _paused.TryGetValue(id, out var paused);
            return paused;
        }

        public bool IsCancelled(Guid id)
        {
            _cancelled.TryGetValue(id, out var cancelled);
            return cancelled;
        }

        public void Update(Framework _)
        {
            // get a message to send, but discard it if we're not ready
            if (!_commands.Reader.TryRead(out var command) || !_ready)
            {
                return;
            }

            // send the message as if it were entered in the chat box
            _xiv.Functions.Chat.SendMessage(command);
        }

        private static string[] ExtractCommands(Micro micro)
        {
            return micro.GetBody()
                .Where(line => line.Trim().Length > 0)
                .Where(line =>
                {
                    var trim = line.Trim();

                    if (trim.StartsWith("#endregion") || trim.StartsWith("#region"))
                    {
                        return true;
                    }

                    return !trim.StartsWith("#");
                })
                .ToArray();
        }

        private static TimeSpan? ExtractWait(ref string command)
        {
            var matches = Wait.Matches(command);
            if (matches.Count == 0)
            {
                return null;
            }

            var match = matches[^1];
            var waitTime = match.Groups[1].Captures[0].Value;

            if (!double.TryParse(waitTime, NumberStyles.Number, CultureInfo.InvariantCulture, out var seconds))
            {
                return null;
            }

            command = Wait.Replace(command, string.Empty);
            return TimeSpan.FromSeconds(seconds);
        }

        private void Login(object? sender, EventArgs args)
        {
            _ready = true;
        }

        private void Logout(object? sender, EventArgs args)
        {
            _ready = false;

            foreach (var id in Running.Keys)
            {
                CancelMicro(id);
            }
        }

        public class MicroInfo
        {
            public Micro Micro { get; private set; }
            public string? CurrentCommand { get; internal set; }
            public string? CurrentRegion { get; internal set; }
            public int CurrentCommandIndex { get; internal set; }
            public int CommandsLength { get; internal set; }
            public DateTime? CurrentCommandStartTime { get; internal set; }
            public TimeSpan? CurrentCommandWait { get; internal set; }
            public DateTime? CurrentRegionStartTime { get; internal set; }
            public TimeSpan? CurrentRegionWait { get; internal set; }

            public TimeSpan CurrentCommandTimeLeft
            {
                get
                {
                    if (CurrentCommandStartTime == null || CurrentCommandWait == null)
                    {
                        return TimeSpan.Zero;
                    }

                    return CurrentCommandStartTime.Value + CurrentCommandWait.Value - DateTime.Now;
                }
            }

            public float TotalProgress
            {
                get
                {
                    return Math.Clamp(Math.Max(CurrentCommandIndex, 0) / (float)Math.Max(CommandsLength, 1), 0, 1);
                }
            }

            public float CurrentCommandProgress
            {
                get
                {
                    if (CurrentCommandStartTime == null || CurrentCommandWait == null)
                    {
                        return 0;
                    }

                    return
                        Math.Clamp((float)InvLerp(
                            CurrentCommandStartTime.Value,
                            CurrentCommandStartTime.Value + CurrentCommandWait.Value,
                            DateTime.Now
                        ), 0, 1);
                }
            }

            public float CurrentRegionProgress
            {
                get
                {
                    if (CurrentRegionStartTime == null || CurrentRegionWait == null)
                    {
                        return 0;
                    }

                    return
                        Math.Clamp((float)InvLerp(
                            CurrentRegionStartTime.Value,
                            CurrentRegionStartTime.Value + CurrentRegionWait.Value,
                            DateTime.Now
                        ), 0, 1);
                }
            }

            public MicroInfo(Micro micro)
            {
                Micro = micro;
            }

            private static double InvLerp(DateTime dtA, DateTime dtB, DateTime dtV)
            {
                var a = dtA;
                var b = dtB;
                var v = dtV;

                return (v - a).TotalMilliseconds / (b - a).TotalMilliseconds;
            }
        }
    }
}
