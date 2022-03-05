using System;
using System.Text;

namespace Microdancer
{
    public static class TimeSpanExtensions
    {
        public static string ToTimeCode(this TimeSpan ts)
        {
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}:{ts.Milliseconds:D3}";
        }
    }
}
