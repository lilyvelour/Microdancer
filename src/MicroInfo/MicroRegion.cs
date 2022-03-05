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
        public int StartLineNumber { get; private set; }
        public int EndLineNumber { get; private set; }

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

        public MicroRegion() : this(null, false) { }

        public MicroRegion(string? name, bool isNamedRegion)
        {
            Name = name ?? string.Empty;
            IsNamedRegion = isNamedRegion;
            IsDefaultRegion = name == null;
        }

        internal MicroCommand AddCommand(MicroCommand command)
        {
            Commands.Add(command);
            StartLineNumber = Commands.Min(c => c.LineNumber);
            EndLineNumber = Commands.Max(c => c.LineNumber);
            WaitTime = TimeSpan.FromMilliseconds(Commands.Sum(c => c.WaitTime.TotalMilliseconds));
            return command;
        }
    }
}
