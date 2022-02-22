using System;
using System.Linq;

namespace Microdancer
{
    public abstract class MicroInfoBase : IMicroTime
    {
        public TimeSpan WaitTime { get; protected set; }
        public DateTime? StartTime { get; internal set; }

        public TimeSpan? GetRemainingTime()
        {
            if (StartTime == null)
            {
                return null;
            }

            return StartTime + WaitTime - DateTime.Now;
        }

        public float GetProgress()
        {
            if (StartTime == null)
            {
                return 0;
            }

            var a = StartTime.Value;
            var b = a + WaitTime;
            var v = DateTime.Now;

            return MathExt.InvLerp(a, b, v);
        }
    }
}
