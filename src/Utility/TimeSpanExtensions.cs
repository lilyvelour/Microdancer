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
            var sb = new StringBuilder();

            var hasDays = false;
            var hasHours = false;
            var hasMinutes = false;

            if ((int)ts.TotalDays > 0)
            {
                hasDays = true;
                sb.Append((int)ts.TotalDays);
                sb.Append(':');
            }

            if ((int)ts.TotalHours > 0)
            {
                hasHours = true;
                sb.Append(ts.Hours.ToString(hasDays ? "D2" : "D1"));
                sb.Append(':');
            }

            if ((int)ts.TotalMinutes > 0)
            {
                hasMinutes = true;
                sb.Append(ts.Minutes.ToString(hasHours ? "D2" : "D1"));
                sb.Append(':');
            }

            if (ts.Seconds > 0)
            {
                if (hasMinutes)
                {
                    sb.Append(ts.Seconds.ToString("D2"));
                }
                else
                {
                    sb.Append(ts.TotalSeconds.ToString("G3"));
                }
            }
            else if (ts.Milliseconds > 0)
            {
                sb.Append(ts.Milliseconds * 0.001);
            }
            else
            {
                sb.Append('0');
            }

            if (!hasMinutes)
            {
                sb.Append(" sec");
            }

            return sb.ToString();
        }
    }
}
