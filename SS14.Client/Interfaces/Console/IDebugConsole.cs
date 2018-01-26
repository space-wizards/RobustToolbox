using System.Collections.Generic;
using SS14.Shared.Console;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Console
{
    public interface IDebugConsole
    {
        IReadOnlyDictionary<string, IConsoleCommand> Commands { get; }

        /// <summary>
        /// Write a line with a specific color to the console window.
        /// </summary>
        void AddLine(string text, ChatChannel channel, Color color);

        void Clear();
    }
}
