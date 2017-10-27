using OpenTK.Graphics;
using System.Collections.Generic;

namespace SS14.Client.Interfaces.Console
{
    public interface IDebugConsole
    {
        IReadOnlyDictionary<string, IConsoleCommand> Commands { get; }

        /// <summary>
        /// Write a line with a specific color to the console window.
        /// </summary>
        void AddLine(string text, Color4 color);

        void Clear();
    }
}
