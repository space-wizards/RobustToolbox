using Robust.Client.Debugging.Overlays;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Light;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Client.Light;

public sealed class LightLevelOverlay : TileFloatDebugOverlay
{
    private LightLevelSystem _system = default!;

    protected override void Init()
    {
        _system = Entity.System<LightLevelSystem>();
    }

    protected override float? GetData(Vector2i indices, Entity<MapGridComponent> grid)
    {
        var pos = Map.GridTileToWorld(grid, grid, indices);
        return _system.CalculateLightLevel(pos);
    }

    // TODO LIGHT LEVEL remove and just use toolshed command
    private sealed class Toggle : IConsoleCommand
    {
        [Dependency] private readonly IDependencyCollection _deps = default!;
        [Dependency] private readonly IOverlayManager _overlay = default!;

        public string Command => "lightlevel";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (_overlay.HasOverlay<LightLevelOverlay>())
            {
                _overlay.RemoveOverlay<LightLevelOverlay>();
                return;
            }

            var o = new LightLevelOverlay();
            _deps.InjectDependencies(o);
            if (o is IPostInjectInit init)
                init.PostInject();
            _overlay.AddOverlay(o);
        }
    }
}
