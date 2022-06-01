using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microdancer
{
    public class MicroInfo : MicroInfoBase
    {
        private static readonly Regex _waitInlineExp =
            new(@"<wait\.(\d+(?:\.\d+)?)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _waitExp =
            new(@"/wait (\d+(?:\.\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private MicroCommand? _currentCommand;
        private bool _isPlaying;

        public Guid Id { get; }
        public Micro Micro { get; }
        public bool IsSingleRegion { get; private set; }
        public bool DriftCompensation { get; private set; } = true;
        public MicroCommand[] Commands { get; } = Array.Empty<MicroCommand>();
        public MicroRegion[] Regions { get; } = Array.Empty<MicroRegion>();
        public MicroCommand[] AllCommands { get; private set; } = Array.Empty<MicroCommand>();
        public MicroRegion[] AllRegions { get; private set; } = Array.Empty<MicroRegion>();

        private readonly TimeSpan _offset;

        public MicroCommand? CurrentCommand
        {
            get { return _currentCommand; }
            set
            {
                if (_currentCommand != value)
                {
                    if (IsPlaying)
                    {
                        _currentCommand?.FinishCommand();
                        value?.StartCommand();
                    }

                    _currentCommand = value;
                }
            }
        }

        public override bool IsPlaying => _isPlaying;
        public override bool IsPaused => CurrentCommand?.IsPaused == true;
        public override TimeSpan CurrentTime =>
            TimeSpan.FromMilliseconds(Commands.Sum(c => c.CurrentTime.TotalMilliseconds)) + _offset;

        public TimeSpan TotalTime { get; }

        public MicroInfo(Micro micro, string? region = null)
        {
            Id = Guid.NewGuid();
            Micro = micro;

            var body = Micro.GetBody().ToArray();
            IsSingleRegion = region != null;

            Commands = ParseCommands(body, region, -1).ToArray();
            Regions = Commands.Select(c => c.Region).Distinct().ToArray();
            WaitTime = TimeSpan.FromMilliseconds(Commands.Sum(c => c.WaitTime.TotalMilliseconds));
            TotalTime = TimeSpan.FromMilliseconds(AllCommands.Sum(c => c.WaitTime.TotalMilliseconds));
        }

        public MicroInfo(Micro micro, int lineNumber)
        {
            Id = Guid.NewGuid();
            Micro = micro;

            var body = Micro.GetBody().ToArray();
            IsSingleRegion = false;

            var commands = ParseCommands(body, null, lineNumber);
            var offsetMs = commands.Sum(
                c =>
                {
                    if (c.LineNumber >= lineNumber)
                        return 0;
                    return (int)c.WaitTime.TotalMilliseconds;
                }
            );
            Commands = commands.Where(c => c.LineNumber >= lineNumber).ToArray();

            Regions = Commands.Select(c => c.Region).Distinct().ToArray();
            WaitTime = TimeSpan.FromMilliseconds(Commands.Sum(c => c.WaitTime.TotalMilliseconds));
            TotalTime = TimeSpan.FromMilliseconds(AllCommands.Sum(c => c.WaitTime.TotalMilliseconds));
            _offset = TimeSpan.FromMilliseconds(offsetMs);
        }

        public void Start()
        {
            _isPlaying = true;
        }

        public void Pause()
        {
            CurrentCommand?.PauseCommand();
        }

        public void Resume()
        {
            CurrentCommand?.ResumeCommand();
        }

        public void Stop()
        {
            CurrentCommand?.StopCommand();
            _isPlaying = false;
        }

        internal void Loop()
        {
            foreach (var command in Commands)
            {
                command.StopCommand();
            }

            CurrentCommand = Commands.FirstOrDefault();
        }

        private IEnumerable<MicroCommand> ParseCommands(string[] body, string? region, int startLineNumber)
        {
            var allCommands = new List<MicroCommand>();

            if (region?.StartsWith(":") == true)
            {
                region = region[1..];
            }

            TimeSpan? defaultWait = null;
            var currentRegion = new MicroRegion(1);

            bool? overrideInclude = null;

            for (var i = 0; i < body.Length; ++i)
            {
                var line = body[i]?.TrimEnd();

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var lineNumber = i + 1;

                if (line.StartsWith("#region ") && line.Length > 8)
                {
                    var regionName = line[8..];
                    var isNamedRegion = false;

                    // Named regions
                    if (regionName.StartsWith(":"))
                    {
                        if (regionName.Length == 1)
                        {
                            continue;
                        }

                        regionName = regionName[1..];
                        isNamedRegion = true;
                    }

                    currentRegion = new MicroRegion(regionName, isNamedRegion, lineNumber);
                }
                else if (line.StartsWith("#endregion") || line.StartsWith("#region"))
                {
                    if (overrideInclude == true && startLineNumber >= 0)
                    {
                        overrideInclude = false;
                        IsSingleRegion = currentRegion?.IsNamedRegion == true;
                    }
                    if (currentRegion != null)
                    {
                        currentRegion.EndLineNumber = lineNumber;
                    }
                    currentRegion = new MicroRegion(lineNumber + 1);
                    continue;
                }
                else if (line.StartsWith("#! disable-drift-comp"))
                {
                    DriftCompensation = false;
                    continue;
                }
                else if (line.StartsWith("#"))
                {
                    continue;
                }
                else
                {
                    if (line.StartsWith("/defaultwait"))
                    {
                        defaultWait = SetDefaultWait(defaultWait, line);
                        continue;
                    }

                    var ind = line.IndexOf("#");
                    if (ind > 0)
                    {
                        line = line[..ind];
                    }

                    var wait = ExtractWait(ref line, defaultWait);
                    var microCommand = new MicroCommand(line, lineNumber, wait, currentRegion);
                    currentRegion.AddCommand(microCommand);

                    // Add to all commands
                    allCommands.Add(microCommand);

                    // Include all commands above the start line number, until we've hit a region boundary
                    if (startLineNumber >= 0 && overrideInclude == null && lineNumber >= startLineNumber)
                    {
                        overrideInclude = true;
                    }

                    if (overrideInclude == true)
                    {
                        yield return microCommand;
                    }
                    else
                    {
                        // Don't include named regions unless they are explicitly executed
                        if (!IsSingleRegion && currentRegion.IsNamedRegion == true)
                        {
                            continue;
                        }

                        // Only include executable commands
                        if (!IsSingleRegion || region == currentRegion.Name)
                        {
                            yield return microCommand;
                        }
                    }
                }
            }

            if (currentRegion != null && currentRegion.EndLineNumber < 0)
            {
                currentRegion.EndLineNumber = body.Length;
            }

            // Get all regions
            AllCommands = allCommands.ToArray();
            AllRegions = AllCommands.Select(c => c.Region).Distinct().ToArray();
        }

        private static TimeSpan ExtractWait(ref string command, TimeSpan? defaultWait)
        {
            defaultWait ??= TimeSpan.FromMilliseconds(10);

            var regex = _waitInlineExp;
            var matches = regex.Matches(command);

            if (matches.Count == 0)
            {
                regex = _waitExp;
                matches = regex.Matches(command);
            }

            if (matches.Count == 0)
            {
                return defaultWait.Value;
            }

            var match = matches[^1];
            var waitTime = match.Groups[1].Captures[0].Value;

            if (!double.TryParse(waitTime, NumberStyles.Number, CultureInfo.InvariantCulture, out var seconds))
            {
                return defaultWait.Value;
            }

            command = regex == _waitExp ? "/wait" : regex.Replace(command, string.Empty);

            // Timed commands
            if (command.StartsWith("/presskey"))
            {
                var split = command.Split();
                if (double.TryParse(split.Last(), out var t))
                {
                    seconds = t;
                }
            }

            // Special commands
            if (
                command == "/defaultwait"
                || command == "/loop"
                || command == "/autocountdown"
                || command == "/autocd"
                || command == "/autoping"
                || command == "/autobusy"
                || command == "/autobussy"
            )
            {
                return TimeSpan.Zero;
            }

            try
            {
                return TimeSpan.FromSeconds(seconds);
            }
            catch
            {
                return defaultWait.Value;
            }
        }

        private static TimeSpan? SetDefaultWait(TimeSpan? defaultWait, string command)
        {
            if (command == "/defaultwait")
            {
                defaultWait = null;
            }
            else
            {
                var defaultWaitStr = command.Split(' ')[^1].Trim();
                if (double.TryParse(defaultWaitStr, out var waitTime))
                {
                    defaultWait = TimeSpan.FromSeconds(waitTime);
                }
            }

            return defaultWait;
        }
    }
}
