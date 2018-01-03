using System;
using System.Collections.Generic;
using SS14.Client.Interfaces.Console;

namespace SS14.Client.Console
{
    internal interface IClientConsole : IDisposable
    {
        /// <summary>
        ///     Initializes the console into a useable state.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Resets the console to a post-initialized state.
        /// </summary>
        void Reset();


        event EventHandler<AddStringArgs> AddString;
        event EventHandler ClearText;

        IReadOnlyDictionary<string, IConsoleCommand> Commands { get; }

        /// <summary>
        ///     Parses console commands (verbs).
        /// </summary>
        /// <param name="text"></param>
        void ProcessCommand(string text);

        void SendServerCommandRequest();
    }
}
