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

        public static string ToSimpleString(this TimeSpan ts)
        {
            if ((int)ts.TotalMinutes < 1)
            {
                return ToSecondsString(ts);
            }

            var sb = new StringBuilder();

            var hasDays = false;
            var hasHours = false;

            if ((int)ts.TotalDays > 0)
            {
                hasDays = true;
                sb.Append((int)ts.TotalDays).Append(':');
            }

            if ((int)ts.TotalHours > 0)
            {
                hasHours = true;
                sb.Append(ts.Hours.ToString(hasDays ? "D2" : "D1")).Append(':');
            }
            sb.Append($"{ts.Minutes.ToString(hasHours ? "D2" : "D1")}:{ts.Seconds:D2}");

            return sb.ToString();
        }

        public static string ToSecondsString(this TimeSpan ts)
        {
            return ToSecondsString(ts.TotalSeconds);
        }

        public static string ToSecondsString(this double t)
        {
            return $"{t:0.###} sec";
        }
    }
}
