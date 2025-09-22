using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared;
using Robust.Shared.Console;
using Robust.Shared.Network.Messages;

namespace Robust.Client.Console;

internal partial class ClientConsoleHost
{
    private readonly Dictionary<int, PendingCompletion> _completionsPending = new();
    private int _completionSeq;


    public async Task<CompletionResult> GetCompletions(List<string> args, string argStr, CancellationToken cancel)
    {
        // Last element is the command currently being typed. May be empty.

        // _logger.Debug($"Running completions: {string.Join(", ", args)}");

        var delay = _cfg.GetCVar(CVars.ConCompletionDelay);
        if (delay > 0)
            await Task.Delay((int)(delay * 1000), cancel);

        return await CalcCompletions(args, argStr, cancel);
    }

    private Task<CompletionResult> CalcCompletions(List<string> args, string argStr, CancellationToken cancel)
    {
        if (args.Count == 1)
        {
            // Typing out command name, handle this ourselves.
            var cmdOptions = CompletionResult.FromOptions(
                AvailableCommands.Values
                    .Where(c => CanExecute(c.Command))
                    .OrderBy(c => c.Command)
                    .Select(c => new CompletionOption(c.Command, c.Description)));

            return Task.FromResult(cmdOptions);
        }

        if (!AvailableCommands.TryGetValue(args[0], out var cmd))
            return Task.FromResult(CompletionResult.Empty);

        return cmd.GetCompletionAsync(LocalShell, args.ToArray()[1..], argStr, cancel).AsTask();
    }

    private Task<CompletionResult> DoServerCompletions(List<string> args, string argStr, CancellationToken cancel)
    {
        var tcs = new TaskCompletionSource<CompletionResult>();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        var seq = _completionSeq++;

        var pending = new PendingCompletion
        {
            Cts = cts,
            Tcs = tcs
        };

        var msg = new MsgConCompletion
        {
            Args = args.ToArray(),
            ArgString = argStr,
            Seq = seq
        };

        cts.Token.Register(() =>
        {
            tcs.SetCanceled(cts.Token);
            cts.Dispose();
            _completionsPending.Remove(seq);
        }, true);

        NetManager.ClientSendMessage(msg);

        _completionsPending.Add(seq, pending);

        return tcs.Task;
    }

    private void ProcessCompletionResp(MsgConCompletionResp message)
    {
        if (!_completionsPending.TryGetValue(message.Seq, out var pending))
            return;

        pending.Cts.Dispose();
        pending.Tcs.SetResult(message.Result);

        _completionsPending.Remove(message.Seq);
    }

    private struct PendingCompletion
    {
        public TaskCompletionSource<CompletionResult> Tcs;
        public CancellationTokenSource Cts;
    }
}
