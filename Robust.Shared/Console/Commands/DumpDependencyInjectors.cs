using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Shared.Console.Commands;

internal sealed class DumpDependencyInjectors : LocalizedCommands
{
    [Dependency] private readonly IDependencyCollection _dependencies = default!;

    public override string Command => "dump_dependency_injectors";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var deps = (DependencyCollection)_dependencies;

        var types = deps.GetCachedInjectorTypes();

        foreach (var type in types)
        {
            shell.WriteLine(type.FullName ?? "");
        }

        shell.WriteLine(Loc.GetString("cmd-dump_dependency_injectors-total-count", ("total", types.Length)));
    }
}
