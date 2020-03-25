using System;
using System.Collections.Generic;

#nullable enable

namespace Robust.Shared.Utility
{
    internal static class CommandParsing
    {
        /// <summary>
        /// Parses a full console command into a list of arguments.
        /// </summary>
        /// <param name="text">Full input string.</param>
        /// <param name="args">List of arguments to write into.</param>
        public static void ParseArguments(ReadOnlySpan<char> text, List<string> args)
        {
            var curStart = -1;
            var inQuotes = false;

            for (var i = 0; i < text.Length; i++)
            {
                if (inQuotes)
                {
                    if (text[i] == '"')
                    {
                        inQuotes = false;
                        args.Add(text[curStart..i].ToString());
                        curStart = -1;
                    }

                    continue;
                }

                if (text[i] == '"')
                {
                    inQuotes = true;
                    curStart = i + 1;
                    continue;
                }

                if (text[i] == ' ')
                {
                    if (curStart != -1)
                    {
                        args.Add(text[curStart..i].ToString());
                    }

                    curStart = -1;

                    continue;
                }

                if (curStart == -1)
                {
                    curStart = i;
                }
            }

            if (curStart != -1)
            {
                args.Add(text[curStart..].ToString());
            }
        }
    }
}
