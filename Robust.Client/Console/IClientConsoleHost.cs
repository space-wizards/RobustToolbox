using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Robust.Client.Console
{
    public interface IClientConsoleHost : IConsoleHost, IDisposable
    {
        event EventHandler<AddStringArgs> AddString;
        event EventHandler<AddFormattedMessageArgs> AddFormatted;

        void AddFormattedLine(FormattedMessage message);

        Task<CompletionResult> GetCompletions(List<string> args, string argStr, CancellationToken cancel);
    }
}
