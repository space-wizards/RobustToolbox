using System;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Robust.Client.Console
{
    public interface IClientConsoleHost : IConsoleHost, IDisposable
    {
        /// <summary>
        /// Initializes the console into a useable state.
        /// </summary>
        void Initialize();

        event EventHandler<AddStringArgs> AddString;
        event EventHandler<AddFormattedMessageArgs> AddFormatted;

        void AddFormattedLine(FormattedMessage message);
    }
}
