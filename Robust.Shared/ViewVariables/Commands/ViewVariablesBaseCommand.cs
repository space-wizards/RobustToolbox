using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Shared.ViewVariables.Commands;

public abstract class ViewVariablesBaseCommand : LocalizedCommands
{
    [Dependency] protected readonly INetManager _netMan = default!;
    [Dependency] protected readonly IViewVariablesManager _vvm = default!;

    protected abstract VVAccess RequiredAccess { get; }

    public override async ValueTask<CompletionResult> GetCompletionAsync(IConsoleShell shell, string[] args, CancellationToken cancel)
    {
        if (args.Length is 0 or > 1)
            return CompletionResult.Empty;

        var path = args[0];

        var opts = new VVListPathOptions() { MinimumAccess = RequiredAccess };

        if (_netMan.IsClient)
        {
            if(path.StartsWith("/c"))
                return CompletionResult.FromOptions(
                    _vvm.ListPath(path[2..], opts)
                        .Select(p => new CompletionOption($"/c{p}", null, CompletionOptionFlags.PartialCompletion)));

            return CompletionResult.FromOptions((await _vvm.ListRemotePath(path, opts))
                .Select(p => new CompletionOption(p, null, CompletionOptionFlags.PartialCompletion))
                .Append(new CompletionOption("/c", "Client-side paths", CompletionOptionFlags.PartialCompletion)));
        }

        return CompletionResult.FromOptions(
            _vvm.ListPath(path, opts)
                .Select(p => new CompletionOption(p, null, CompletionOptionFlags.PartialCompletion)));
    }
}
