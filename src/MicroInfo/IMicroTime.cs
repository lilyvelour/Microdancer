using System;

namespace Microdancer
{
    public interface IMicroTime
    {
        TimeSpan WaitTime { get; }
        TimeSpan CurrentTime { get; }
        TimeSpan RemainingTime { get; }
        bool IsPlaying { get; }
        bool IsPaused { get; }

        float GetProgress();
    }
}
