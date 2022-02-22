using System;
using System.Collections.Generic;
using System.Linq;

namespace Microdancer
{
    public class MicroRegion : MicroInfoBase
    {
        public string Name { get; }
        public bool IsNamedRegion { get; }
        public int StartLineNumber { get; private set; }
        public int EndLineNumber { get; private set; }

        public IList<MicroCommand> Commands { get; } = new List<MicroCommand>();

        public MicroRegion(string name, bool isNamedRegion)
        {
            Name = name;
            IsNamedRegion = isNamedRegion;
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
