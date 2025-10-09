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
/// This is an abstract helper class that can be used to create simple debug overlays that need to render tile based data.
/// </summary>
[UsedImplicitly]
public abstract class TileDebugOverlay : Overlay, IPostInjectInit
{
    [Dependency] protected readonly IEntityManager Entity = default!;
    [Dependency] protected readonly IEyeManager Eye = default!;
    [Dependency] protected readonly IMapManager MapMan = default!;
    [Dependency] protected readonly IInputManager Input = default!;
    [Dependency] protected readonly IUserInterfaceManager Ui = default!;
    [Dependency] protected readonly IResourceCache Cache = default!;

    protected SharedTransformSystem Transform = default!;
    protected MapSystem Map = default!;
    protected EntityLookupSystem Lookup = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    protected Font Font = default!;
    protected List<Entity<MapGridComponent>> Grids = new();

    public void PostInject()
    {
        Transform = Entity.System<SharedTransformSystem>();
        Map = Entity.System<MapSystem>();
        Lookup = Entity.System<EntityLookupSystem>();
        var font = Cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf");
        Font = new VectorFont(font, 8);
        Init();
    }

    protected virtual void Init()
    {
    }

    protected internal override void Draw(in OverlayDrawArgs args)
    {
        Grids.Clear();
        if (args.Viewport.Eye?.Position.MapId is not {} map || map == MapId.Nullspace)
            return;

        MapMan.FindGridsIntersecting(map, args.WorldBounds, ref Grids);

        foreach (var grid in Grids)
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

        Grids.Clear();
    }

    protected virtual void DrawScreen(in OverlayDrawArgs args, Entity<MapGridComponent> grid)
    {
        var handle = args.ScreenHandle;
        var (_, _, matrix, invMatrix) = Transform.GetWorldPositionRotationMatrixWithInv(grid.Owner);
        var gridBounds = invMatrix.TransformBox(args.WorldBounds).Enlarged(grid.Comp.TileSize * 2);
        var tilesEnumerator = Map.GetLocalTilesEnumerator(grid, grid, gridBounds);
        while (tilesEnumerator.MoveNext(out var tile))
        {
            var tileBounds = Lookup.GetLocalBounds(tile, grid.Comp.TileSize);
            if (!gridBounds.Intersects(tileBounds))
                continue;
            var screenTileCentre = Eye.WorldToScreen(Vector2.Transform(tileBounds.Center, matrix));
            DrawTileText(handle, screenTileCentre, tile.GridIndices, grid);
        }

        // Draw mouse tooltip
        DrawTooltip(handle);

    }

    protected virtual void DrawTooltip(DrawingHandleScreen handle)
    {
        var mousePos = Input.MouseScreenPosition;
        if (!mousePos.IsValid)
            return;

        if (Ui.MouseGetControl(mousePos) is not IViewportControl viewport)
            return;

        var coords = viewport.PixelToMap(mousePos.Position);

        if (!MapMan.TryFindGridAt(coords, out var grid, out var comp))
            return;

        var local = Map.WorldToLocal(grid, comp, coords.Position);
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
        if (GetTooltip(mouseLocal, indices, grid) is not { } text)
            return;

        var lineHeight = Font.GetLineHeight(1f);
        var offset = new Vector2(0, lineHeight);
        handle.DrawString(Font, mouseScreen - offset, text);
    }

    protected virtual void DrawTileText(DrawingHandleScreen handle, Vector2 tileCentre, Vector2i indices, Entity<MapGridComponent> grid)
    {
        if (GetText(indices, grid) is {} text)
            handle.DrawString(Font, tileCentre, text);
    }

    protected virtual void DrawWorld(in OverlayDrawArgs args, Entity<MapGridComponent> grid)
    {
        var handle = args.WorldHandle;
        var (_, _, matrix, invMatrix) = Transform.GetWorldPositionRotationMatrixWithInv(grid.Owner);
        var gridBounds = invMatrix.TransformBox(args.WorldBounds).Enlarged(grid.Comp.TileSize * 2);
        var tilesEnumerator = Map.GetLocalTilesEnumerator(grid, grid, gridBounds);
        while (tilesEnumerator.MoveNext(out var tile))
        {
            handle.SetTransform(matrix);
            var tileBounds = Lookup.GetLocalBounds(tile, grid.Comp.TileSize);
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

    /// <summary>
    /// Get text that will be rendered in a grid tile.
    /// </summary>
    protected abstract string? GetText(Vector2i indices, Entity<MapGridComponent> grid);

    /// <summary>
    /// Get tooltip text that will be shown next to the mouse.
    /// </summary>
    /// <param name="mousePos">The mouse's position relative to the grid.</param>
    /// <param name="gridIndices">The grid indices corresponding to the mouse's position</param>
    /// <param name="grid">The grid that the mouse is over.</param>
    protected abstract string? GetTooltip(Vector2 mousePos, Vector2i indices, Entity<MapGridComponent> grid);

    /// <summary>
    /// Get a border & fill color that will be used to draw a grid tile.
    /// </summary>
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
        return GetData(indices, grid)?.ToString("F2");
    }

    protected override string? GetTooltip(Vector2 mousePos, Vector2i indices, Entity<MapGridComponent> grid)
    {
        return GetData(indices, grid)?.ToString("F2");
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
