using System.Collections.Generic;

namespace SS14.Shared.Utility
{
    public class CommandParsing
    {
        /// <summary>
        /// Command parsing func
        /// </summary>
        /// <param name="text">full input string</param>
        /// <param name="args">List of arguments, including the command as #0</param>
        static public void ParseArguments(string text, List<string> args)
        {
            string buf = "";
            bool inquotes = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '*')
                    continue;
                else if (inquotes && text[i] == '"')
                {
                    inquotes = false;
                    args.Add(buf);
                    buf = "";
                    i++; //skip the following space.
                    continue;
                }
                else if (!inquotes && text[i] == '"')
                {
                    inquotes = true;
                    continue;
                }
                else if (text[i] == ' ' && !inquotes)
                {
                    args.Add(buf);
                    buf = "";
                    continue;
                }
                else
                {
                    buf += text[i];
                    continue;
                }
            }

            if (buf != "")
                args.Add(buf);
        }
    }
}