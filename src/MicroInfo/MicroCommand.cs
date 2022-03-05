using System;
using System.Diagnostics;

namespace Microdancer
{
    public class MicroCommand : MicroInfoBase
    {
        private readonly Stopwatch _stopwatch = new();
        private bool _isPaused;

        public string Text { get; }
        public int LineNumber { get; }
        public MicroRegion Region { get; }

        public override bool IsPlaying => _stopwatch.IsRunning || IsPaused;
        public override bool IsPaused => _isPaused;
        public override TimeSpan CurrentTime => _stopwatch.Elapsed;

        public MicroCommand(string text, int lineNumber, TimeSpan waitTime, MicroRegion region)
        {
            Text = text;
            LineNumber = lineNumber;
            WaitTime = waitTime;
            Region = region;
        }

        internal void StartCommand()
        {
            _isPaused = false;
            _stopwatch.Restart();
        }

        internal void PauseCommand()
        {
            _isPaused = true;
            _stopwatch.Stop();
        }

        internal void ResumeCommand()
        {
            _isPaused = false;
            _stopwatch.Start();
        }

        internal void FinishCommand()
        {
            _stopwatch.Stop();
        }

        internal void StopCommand()
        {
            _isPaused = false;
            _stopwatch.Reset();
        }
    }
}
