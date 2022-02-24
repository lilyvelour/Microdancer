using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microdancer
{
    public class MicroInfo : MicroInfoBase
    {
        public Guid Id { get; }
        public Micro Micro { get; }
        public int LineCount { get; }
        public bool IsSingleRegion { get; }
        public MicroCommand[] Commands { get; } = Array.Empty<MicroCommand>();
        public MicroCommand? CurrentCommand { get; internal set; }
        internal bool WasCancelled { get; set; }

        private static readonly Regex _waitExp =
            new(@"/wait (\d+(?:\.\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _waitInlineExp =
            new(@"<wait\.(\d+(?:\.\d+)?)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public MicroInfo(Micro micro, string? region)
        {
            Id = Guid.NewGuid();
            Micro = micro;

            var body = Micro.GetBody().ToArray();
            LineCount = body.Length;
            IsSingleRegion = region != null;

            Commands = ParseCommands(body, region).ToArray();

            WaitTime = TimeSpan.FromMilliseconds(Commands.Sum(c => c.WaitTime.TotalMilliseconds));
        }

        private IEnumerable<MicroCommand> ParseCommands(string[] body, string? region)
        {
            if (region?.StartsWith(":") == true)
            {
                region = region[1..];
            }

            TimeSpan? defaultWait = null;
            MicroRegion? currentRegion = null;

            for (var i = 0; i < body.Length; ++i)
            {
                var line = body[i]?.Trim();

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
                    currentRegion = null;
                    continue;
                }
                else if (line.StartsWith("#"))
                {
                    continue;
                }
                else if (region == null || currentRegion == null || region == currentRegion?.Name)
                {
                    // Don't include named regions unless they are explicitly executed
                    if (region == null && currentRegion?.IsNamedRegion == true)
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
                    currentRegion?.AddCommand(microCommand);

                    yield return microCommand;
                }
            }
        }

        private static TimeSpan ExtractWait(ref string command, TimeSpan? defaultWait)
        {
            var re = _waitInlineExp;
            var matches = re.Matches(command);
            if (matches.Count == 0)
            {
                re = _waitExp;
                matches = re.Matches(command);
            }

            if (matches.Count == 0)
            {
                return defaultWait ?? TimeSpan.FromMilliseconds(10);
            }

            var match = matches[^1];
            var waitTime = match.Groups[1].Captures[0].Value;

            if (!double.TryParse(waitTime, NumberStyles.Number, CultureInfo.InvariantCulture, out var seconds))
            {
                return defaultWait ?? TimeSpan.FromMilliseconds(10);
            }

            command = re.Replace(command, string.Empty);
            return TimeSpan.FromSeconds(seconds);
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
