using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Physics;

[InjectDependencies]
public sealed partial class GridSplitVisualCommand : LocalizedCommands
{
    [Dependency] private IEntitySystemManager _systemManager = default!;

    public override string Command => SharedGridFixtureSystem.ShowGridNodesCommand;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = _systemManager.GetEntitySystem<GridFixtureSystem>();
        system.EnableDebug ^= true;
        shell.WriteLine($"Toggled gridsplit node visuals to {system.EnableDebug}");
    }
}
