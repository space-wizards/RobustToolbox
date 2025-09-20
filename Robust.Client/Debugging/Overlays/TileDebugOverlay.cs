using System;
using System.Collections.Generic;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Client.Debugging.Overlays;

/// <summary>
/// This is a base class for use by any debug overlays that need to render tile based data.
/// </summary>
[UsedImplicitly]
public abstract class TileDebugOverlay : Overlay, IPostInjectInit
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IResourceCache _cache = default!;

    private SharedTransformSystem _transform = default!;
    private MapSystem _map = default!;
    private EntityLookupSystem _lookup = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    private Font _font = default!;
    private List<Entity<MapGridComponent>> _grids = new();

    public void PostInject()
    {
        _transform = _entity.System<SharedTransformSystem>();
        _map = _entity.System<MapSystem>();
        _lookup = _entity.System<EntityLookupSystem>();
        var font = _cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf");
        _font = new VectorFont(font, 8);
    }

    protected internal override void Draw(in OverlayDrawArgs args)
    {
        _grids.Clear();
        if (args.Viewport.Eye?.Position.MapId is not {} map || map == MapId.Nullspace)
            return;

        _mapMan.FindGridsIntersecting(map, args.WorldBounds, ref _grids);

        foreach (var grid in _grids)
        {
            switch (args.Space)
            {
                case OverlaySpace.ScreenSpace:
                    DrawScreen(args, grid);
                    break;
                case OverlaySpace.WorldSpace:
                    DrawWorld(args, grid);
                    break;
            }
        }

        _grids.Clear();
    }

    protected virtual void DrawScreen(in OverlayDrawArgs args, Entity<MapGridComponent> grid)
    {
        var handle = args.ScreenHandle;
        var (_, _, matrix, invMatrix) = _transform.GetWorldPositionRotationMatrixWithInv(grid.Owner);
        var gridBounds = invMatrix.TransformBox(args.WorldBounds).Enlarged(grid.Comp.TileSize * 2);
        var tilesEnumerator = _map.GetLocalTilesEnumerator(grid, grid, gridBounds);
        while (tilesEnumerator.MoveNext(out var tile))
        {
            var tileBounds = _lookup.GetLocalBounds(tile, grid.Comp.TileSize);
            if (!gridBounds.Intersects(tileBounds))
                continue;
            var screenTileCentre = _eye.WorldToScreen(Vector2.Transform(tileBounds.Center, matrix));
            DrawTileText(handle, screenTileCentre, tile.GridIndices, grid);
        }

        // Draw mouse tooltip
        DrawTooltip(handle);

    }

    protected virtual void DrawTooltip(DrawingHandleScreen handle)
    {
        var mousePos = _input.MouseScreenPosition;
        if (!mousePos.IsValid)
            return;

        if (_ui.MouseGetControl(mousePos) is not IViewportControl viewport)
            return;

        var coords = viewport.PixelToMap(mousePos.Position);

        if (!_mapMan.TryFindGridAt(coords, out var grid, out var comp))
            return;

        var local = _map.WorldToLocal(grid, comp, coords.Position);
        var x = (int) Math.Floor(local.X / comp.TileSize);
        var y = (int) Math.Floor(local.Y / comp.TileSize);
        var indices = new Vector2i(x, y);

        DrawTooltip(handle, mousePos.Position, local, indices, (grid, comp));
    }

    /// <summary>
    /// Draw a tooltip around the mouse
    /// </summary>
    /// <param name="mouseScreen">The mouse's screen coordinates</param>
    /// <param name="mouseLocal">The mouse's local grid coordinates</param>
    /// <param name="indices">The mouse's tile indices</param>
    /// <param name="grid">The grid that the mouse is hovering over</param>
    protected virtual void DrawTooltip(DrawingHandleScreen handle, Vector2 mouseScreen, Vector2 mouseLocal, Vector2i indices, Entity<MapGridComponent> grid)
    {
        if (GetTooltip(indices, grid) is { } text)
            handle.DrawString(_font, mouseScreen, text);
    }

    protected virtual void DrawTileText(DrawingHandleScreen handle, Vector2 tileCentre, Vector2i indices, Entity<MapGridComponent> grid)
    {
        if (GetText(indices, grid) is {} text)
            handle.DrawString(_font, tileCentre, text);
    }

    protected virtual void DrawWorld(in OverlayDrawArgs args, Entity<MapGridComponent> grid)
    {
        var handle = args.WorldHandle;
        var (_, _, matrix, invMatrix) = _transform.GetWorldPositionRotationMatrixWithInv(grid.Owner);
        var gridBounds = invMatrix.TransformBox(args.WorldBounds).Enlarged(grid.Comp.TileSize * 2);
        var tilesEnumerator = _map.GetLocalTilesEnumerator(grid, grid, gridBounds);
        while (tilesEnumerator.MoveNext(out var tile))
        {
            handle.SetTransform(matrix);
            var tileBounds = _lookup.GetLocalBounds(tile, grid.Comp.TileSize);
            if (gridBounds.Intersects(tileBounds))
                DrawTile(handle, tileBounds, tile.GridIndices, grid);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }

    protected virtual void DrawTile(DrawingHandleWorld handle, Box2 tile, Vector2i indices, Entity<MapGridComponent> grid)
    {
        if (GetColor(indices, grid) is not { } color)
            return;

        handle.DrawRect(tile, color.Border, filled: false);
        handle.DrawRect(tile, color.Fill, filled: true);
    }

    protected abstract string? GetText(Vector2i indices, Entity<MapGridComponent> grid);
    protected abstract string? GetTooltip(Vector2i indices, Entity<MapGridComponent> grid);
    protected abstract (Color Fill, Color Border)? GetColor(Vector2i indices, Entity<MapGridComponent> grid);
}

/// <summary>
/// Variant of <see cref="TileDebugOverlay"/> that exists to draw simple float information for each tile.
/// </summary>
public abstract class TileFloatDebugOverlay : TileDebugOverlay
{
    protected virtual float MinValue => 0;
    protected virtual float MaxValue => 1;
    protected abstract float? GetData(Vector2i indices, Entity<MapGridComponent> grid);

    protected override string? GetText(Vector2i indices, Entity<MapGridComponent> grid)
    {
        return GetData(indices, grid)?.ToString();
    }

    protected override string? GetTooltip(Vector2i indices, Entity<MapGridComponent> grid)
    {
        return GetData(indices, grid)?.ToString();
    }

    protected override (Color Fill, Color Border)? GetColor(Vector2i indices, Entity<MapGridComponent> grid)
    {
        if (GetData(indices, grid) is not { } value)
            return null;

        var color = Gradient(value, MinValue, MaxValue);
        return (color.WithAlpha(0.2f), color);
    }

    /// <summary>
    /// Simple yellow -> orange -> red gradient.
    /// </summary>
    public Color Gradient(float value, float min, float max)
    {
        // map min to 1, max to 0
        value = (value - min) / (max - min);
        return value < 0.5f
            ? Color.InterpolateBetween(Color.Yellow, Color.Orange, value * 2)
            : Color.InterpolateBetween(Color.Orange, Color.Red, (value - 0.5f) * 2);
    }
}
