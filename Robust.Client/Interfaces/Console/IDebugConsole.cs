using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared;
using Robust.Shared.Console;
using Robust.Client.Utility;
using Robust.Shared.Utility;

namespace Robust.Client.Interfaces.Console
{
    public interface IDebugConsole
    {
        IReadOnlyDictionary<string, IConsoleCommand> Commands { get; }

        /// <summary>
        /// Write a line with a specific color to the console window.
        /// </summary>
        void AddLine(string text, Color color);

        void AddLine(string text);

        void AddFormattedLine(FormattedMessage message);

        void Clear();
    }
}
