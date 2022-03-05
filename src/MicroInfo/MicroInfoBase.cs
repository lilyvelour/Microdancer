using System.Diagnostics;
using System;

namespace Microdancer
{
    public abstract class MicroInfoBase : IMicroTime
    {
        public TimeSpan WaitTime { get; protected set; }
        public TimeSpan RemainingTime => WaitTime - CurrentTime;
        public abstract bool IsPlaying { get; }
        public abstract bool IsPaused { get; }

        public abstract TimeSpan CurrentTime { get; }

        public float GetProgress()
        {
            return MathExt.InvLerp(TimeSpan.Zero, WaitTime, CurrentTime);
        }
    }
}
