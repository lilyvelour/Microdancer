using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microdancer
{
    public class MicroInfo : MicroInfoBase
    {
        private static readonly Regex _waitExp =
            new(@"/wait (\d+(?:\.\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _waitInlineExp =
            new(@"<wait\.(\d+(?:\.\d+)?)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private MicroCommand? _currentCommand;
        private bool _isPlaying;

        public Guid Id { get; }
        public Micro Micro { get; }
        public bool IsSingleRegion { get; }
        public MicroCommand[] Commands { get; } = Array.Empty<MicroCommand>();
        public MicroRegion[] Regions { get; } = Array.Empty<MicroRegion>();

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

        public MicroInfo(Micro micro, string? region = null)
        {
            Id = Guid.NewGuid();
            Micro = micro;

            var body = Micro.GetBody().ToArray();
            IsSingleRegion = region != null;

            Commands = ParseCommands(body, region).ToArray();
            Regions = Commands.Select(c => c.Region).Distinct().ToArray();
            WaitTime = TimeSpan.FromMilliseconds(Commands.Sum(c => c.WaitTime.TotalMilliseconds));
        }

        public MicroInfo(Micro micro, int lineNumber)
        {
            Id = Guid.NewGuid();
            Micro = micro;

            var body = Micro.GetBody().ToArray();
            IsSingleRegion = false;

            var commands = ParseCommands(body, null);
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

        private IEnumerable<MicroCommand> ParseCommands(string[] body, string? region)
        {
            if (region?.StartsWith(":") == true)
            {
                region = region[1..];
            }

            TimeSpan? defaultWait = null;
            MicroRegion currentRegion = new();

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

                    currentRegion = new MicroRegion(regionName, isNamedRegion);
                }
                else if (line.StartsWith("#endregion") || line.StartsWith("#region"))
                {
                    currentRegion = new MicroRegion();
                    continue;
                }
                else if (line.StartsWith("#"))
                {
                    continue;
                }
                else if (region == null || region == currentRegion.Name)
                {
                    // Don't include named regions unless they are explicitly executed
                    if (region == null && currentRegion.IsNamedRegion == true)
                    {
                        continue;
                    }

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

                    yield return microCommand;
                }
            }
        }

        private static TimeSpan ExtractWait(ref string command, TimeSpan? defaultWait)
        {
            defaultWait ??= TimeSpan.FromMilliseconds(10);

            var re = _waitInlineExp;
            var matches = re.Matches(command);

            if (matches.Count == 0)
            {
                re = _waitExp;
                matches = re.Matches(command);
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

            command = re == _waitExp ? "/wait" : re.Replace(command, string.Empty);

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
