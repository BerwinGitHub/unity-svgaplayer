using System.Text;
using UnityEngine;

namespace Bo.SVGA
{
    public static class SVGALog
    {
        public static bool DEBUG = true;
        public static string TAG = "[SVGAPlayer]: ";
        public static ISVGAPlayerProvider Provider;

        private static readonly StringBuilder StringBuilder = new StringBuilder();

        public static void SetProvider(ISVGAPlayerProvider provider)
        {
            Provider = provider;
        }

        public static void Log(string format, params object[] args)
        {
            if (!DEBUG) return;
            var text = Format(format, args);
            Provider?.Log(text);
        }

        public static void LogError(string format, params object[] args)
        {
            if (!DEBUG) return;
            var text = Format(format, args);
            Provider?.LogError(text);
        }

        public static void LogWarning(string format, params object[] args)
        {
            if (!DEBUG) return;
            var text = Format(format, args);
            Provider?.LogWarning(text);
        }


        private static string Format(string format, params object[] args)
        {
            StringBuilder.Clear();
            StringBuilder.Append(TAG);
            StringBuilder.Append(" ");
            if (args != null && args.Length > 0)
            {
                StringBuilder.AppendFormat(format, args);
            }
            else
            {
                StringBuilder.Append(format);
            }

            var text = StringBuilder.ToString();
            return text;
        }
    }
}