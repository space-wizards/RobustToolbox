using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.Collections;

namespace Robust.Shared.Utility
{
    public static class CommandParsing
    {
        /// <summary>
        /// Parses a full console command into a list of arguments.
        /// </summary>
        /// <param name="text">Full input string.</param>
        /// <param name="args">List of arguments to write into.</param>
        public static void ParseArguments(ReadOnlySpan<char> text, List<string> args)
        {
            var ranges = new ValueList<(int, int)>();
            ParseArguments(text, args, ref ranges);
        }

        internal static void ParseArguments(
            ReadOnlySpan<char> text,
            List<string> args,
            ref ValueList<(int start, int end)> ranges)
        {
            var sb = new StringBuilder();
            var inQuotes = false;
            var startPos = -1;

            for (var i = 0; i < text.Length; i++)
            {
                var chr = text[i];
                if (chr == '\\')
                {
                    i += 1;
                    startPos = i;
                    if (i == text.Length)
                    {
                        sb.Append('\\');
                        break;
                    }

                    sb.Append(text[i]);
                    continue;
                }

                if (chr == '"')
                {
                    if (inQuotes)
                    {
                        args.Add(sb.ToString());
                        sb.Clear();
                        ranges.Add((startPos, i + 1));
                        startPos = -1;
                    }
                    else
                    {
                        if (startPos < 0)
                            startPos = i;
                    }

                    inQuotes = !inQuotes;
                    continue;
                }

                if (chr == ' ' && !inQuotes)
                {
                    if (sb.Length != 0)
                    {
                        args.Add(sb.ToString());
                        sb.Clear();
                        ranges.Add((startPos, i));
                        startPos = -1;
                    }

                    continue;
                }

                if (startPos < 0)
                    startPos = i;

                sb.Append(chr);
            }

            if (sb.Length != 0)
            {
                args.Add(sb.ToString());
                ranges.Add((startPos, text.Length));
            }
        }

        public static string Escape(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
