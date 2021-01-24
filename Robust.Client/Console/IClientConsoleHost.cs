using System;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Console
{
    public interface IClientConsoleHost : IConsoleHost, IDisposable
    {
        /// <summary>
        /// Initializes the console into a useable state.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Resets the console to a post-initialized state.
        /// </summary>
        void Reset();

        event EventHandler<AddStringArgs> AddString;
        event EventHandler<AddFormattedMessageArgs> AddFormatted;
        event EventHandler ClearText;

        /// <summary>
        /// Parses console commands (verbs).
        /// </summary>
        /// <param name="text"></param>
        void ProcessCommand(string text);

        void SendServerCommandRequest();

        /// <summary>
        /// Write a line with a specific color to the console window.
        /// </summary>
        void AddLine(string text, Color color);

        void AddLine(string text);

        void AddFormattedLine(FormattedMessage message);

        void Clear();

        void ExecuteCommand(string command);

        /// <summary>
        /// Sends a command directly to the server.
        /// </summary>
        void SendServerConsoleCommand(string text);
    }
}
