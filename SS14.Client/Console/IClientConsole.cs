using System;
using System.Collections.Generic;
using SS14.Client.Interfaces.Console;

namespace SS14.Client.Console
{
    interface IClientConsole : IDisposable
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

        void ProcessCommand(string text);

        void SendServerCommandRequest();
    }
}
