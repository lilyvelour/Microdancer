using System;

namespace Microdancer
{
    public interface IMicroTime
    {
        TimeSpan WaitTime { get; }

        DateTime? StartTime { get; }

        float GetProgress();
        TimeSpan? GetRemainingTime();
    }
}
