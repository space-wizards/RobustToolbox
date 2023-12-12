using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;

namespace Robust.Client.Graphics;

public abstract class GridOverlay : Overlay, IGridOverlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceGrids;

    public Entity<MapGridComponent> Grid { get; set; }

    public bool RequiresFlush { get; set; }
}
