using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;

namespace Robust.Client.Graphics;

/// <summary>
/// Marks this overlay as implementing per-grid rendering.
/// </summary>
public interface IGridOverlay
{
    Entity<MapGridComponent> Grid { get; set; }

    /// <summary>
    /// Should we flush the render or can we keep going.
    /// </summary>
    public bool RequiresFlush { get; set; }
}

