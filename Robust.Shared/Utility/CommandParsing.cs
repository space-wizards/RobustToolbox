using System;
using System.Collections.Generic;

#nullable enable

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
            var buf = "";
            var inQuotes = false;

            for (var i = 0; i < text.Length; i++)
            {
                var chr = text[i];
                if (chr == '\\')
                {
                    i += 1;
                    if (i == text.Length)
                    {
                        buf += "\\";
                        break;
                    }

                    buf += text[i];
                    continue;
                }

                if (chr == '"')
                {
                    if (inQuotes)
                    {
                        args.Add(buf);
                        buf = "";
                    }

                    inQuotes = !inQuotes;
                    continue;
                }

                if (chr == ' ' && !inQuotes)
                {
                    if (buf != "")
                    {
                        args.Add(buf);
                        buf = "";
                    }
                    continue;
                }

                buf += chr;
            }

            if (buf != "")
            {
                args.Add(buf);
            }
        }

        public static string Escape(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
