using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Physics;

public sealed class GridSplitVisualCommand : IConsoleCommand
{
    public string Command => SharedGridFixtureSystem.ShowGridNodesCommand;
    public string Description => "Shows the nodes for grid split purposes";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GridFixtureSystem>();
        system.EnableDebug ^= true;
        shell.WriteLine($"Toggled gridsplit node visuals");
    }
}
