using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Console.Commands;

public sealed class ArchTrimCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public string Command => "arch_trim";
    public string Description => "Runs TrimExcess on arch";
    public string Help => Command;
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _entManager.CleanupArch();
    }
}
