using System;
using System.Collections.Generic;
using System.Linq;

namespace Microdancer
{
    public class MicroRegion : MicroInfoBase
    {
        public string Name { get; }
        public bool IsNamedRegion { get; }
        public bool IsDefaultRegion { get; }
        public int StartLineNumber { get; }
        public int EndLineNumber { get; internal set; } = -1;
        public int CommandStartLineNumber { get; private set; }
        public int CommandEndLineNumber { get; private set; }

        public IList<MicroCommand> Commands { get; } = new List<MicroCommand>();

        public override bool IsPlaying => Commands.Any(c => c.IsPlaying);
        public override bool IsPaused => Commands.Any(c => c.IsPaused);

        public override TimeSpan CurrentTime
        {
            get
            {
                var t = TimeSpan.Zero;
                var valid = false;

                foreach (var command in Commands)
                {
                    if (command.IsPlaying)
                    {
                        valid = true;
                        t += command.CurrentTime;
                        break;
                    }
                    else
                    {
                        t += command.WaitTime;
                    }
                }

                return valid ? t : TimeSpan.Zero;
            }
        }

        public MicroRegion(int lineNumber) : this(null, false, lineNumber) { }

        public MicroRegion(string? name, bool isNamedRegion, int lineNumber)
        {
            Name = name ?? string.Empty;
            IsDefaultRegion = name == null;
            IsNamedRegion = isNamedRegion && !IsDefaultRegion;
            StartLineNumber = lineNumber;
        }

        internal MicroCommand AddCommand(MicroCommand command)
        {
            Commands.Add(command);
            CommandStartLineNumber = Commands.Min(c => c.LineNumber);
            CommandEndLineNumber = Commands.Max(c => c.LineNumber);
            WaitTime = TimeSpan.FromMilliseconds(Commands.Sum(c => c.WaitTime.TotalMilliseconds));
            return command;
        }
    }
}
