using System.Collections.Generic;
using SS14.Shared.Maths;
using SS14.Shared;
using SS14.Shared.Console;

namespace SS14.Client.Interfaces.Console
{
    public interface IDebugConsole
    {
        IReadOnlyDictionary<string, IConsoleCommand> Commands { get; }

        /// <summary>
        /// Write a line with a specific color to the console window.
        /// </summary>
        void AddLine(string text, ChatChannel channel, Color color);

        void AddLine(string text, Color color);

        void AddLine(string text);

        void Clear();
    }
}
