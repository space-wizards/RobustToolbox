using System;

namespace Robust.Shared.Utility
{
    public static class ByteHelpers
    {
        public static string FormatKibibytes(long bytes)
        {
            return $"{bytes / 1024} KiB";
        }

        public static string FormatBytes(long bytes)
        {
            double d = bytes;
            var i = 0;
            for (; i < ByteSuffixes.Length && d >= 1024; i++)
            {
                d /= 1024;
            }

            return $"{Math.Round(d, 2)} {ByteSuffixes[i]}";
        }

        private static readonly string[] ByteSuffixes =
        {
            "B",
            "KiB",
            "MiB",
            "GiB",
            "TiB",
            "PiB",
            "EiB",
            "ZiB",
            "YiB"
        };
    }
}
