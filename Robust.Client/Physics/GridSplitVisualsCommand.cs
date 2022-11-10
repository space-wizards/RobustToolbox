using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Physics;

public sealed class GridSplitVisualCommand : LocalizedCommands
{
    public override string Command => SharedGridFixtureSystem.ShowGridNodesCommand;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GridFixtureSystem>();
        system.EnableDebug ^= true;
        shell.WriteLine($"Toggled gridsplit node visuals to {system.EnableDebug}");
    }
}
