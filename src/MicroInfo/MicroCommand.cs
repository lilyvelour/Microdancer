using System;

namespace Microdancer
{
    public class MicroCommand : MicroInfoBase
    {
        public string Text { get; }
        public int LineNumber { get; }
        public MicroRegion? Region { get; }

        public MicroCommand(string text, int lineNumber, TimeSpan waitTime, MicroRegion? region)
        {
            Text = text;
            LineNumber = lineNumber;
            WaitTime = waitTime;
            Region = region;
        }
    }
}
