using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;

namespace SS14.Client.Interfaces.Console
{
    public interface IDebugConsole
    {
        IDictionary<string, IConsoleCommand> Commands { get; }

        /// <summary>
        /// Write a line with a specific color to the console window.
        /// </summary>
        void AddLine(string text, Color color);

        void Clear();
    }
}
