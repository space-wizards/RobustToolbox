using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Client.Map;

/// <summary>
/// Draws border sprites for tiles that support them.
/// </summary>
public sealed class TileEdgeOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly IMapManager _mapManager;
    private readonly IResourceCache _resource;
    private readonly ITileDefinitionManager _tileDefManager;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    private List<Entity<MapGridComponent>> _grids = new();

    public TileEdgeOverlay(IEntityManager entManager, IMapManager mapManager, IResourceCache resource, ITileDefinitionManager tileDefManager)
    {
        _entManager = entManager;
        _mapManager = mapManager;
        _resource = resource;
        _tileDefManager = tileDefManager;
        ZIndex = -1;
    }

    protected internal override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        _grids.Clear();
        _mapManager.FindGridsIntersecting(args.MapId, args.WorldBounds, ref _grids);

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        foreach (var grid in _grids)
        {
            var tileSize = grid.Comp.TileSize;
            var tileDimensions = new Vector2(tileSize, tileSize);
            var xform = xformQuery.GetComponent(grid);
            args.WorldHandle.SetTransform(xform.WorldMatrix);

            foreach (var tileRef in grid.Comp.GetTilesIntersecting(args.WorldBounds, false))
            {
                var tileDef = _tileDefManager[tileRef.Tile.TypeId];

                if (tileDef.EdgeSprites.Count == 0)
                    continue;

                // Get what tiles border us to determine what sprites we need to draw.
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        var neighborIndices = new Vector2i(tileRef.GridIndices.X + x, tileRef.GridIndices.Y + y);
                        var neighborTile = grid.Comp.GetTileRef(neighborIndices);
                        var neighborDef = _tileDefManager[neighborTile.Tile.TypeId];

                        // If it's the same tile then no edge to be drawn.
                        if (tileRef.Tile.TypeId == neighborTile.Tile.TypeId)
                            continue;

                        // Don't draw if the the neighbor tile edges should draw over us (or if we have the same priority)
                        if (neighborDef.EdgeSprites.Count != 0 && neighborDef.EdgeSpritePriority >= tileDef.EdgeSpritePriority)
                            continue;

                        var direction = new Vector2i(x, y).AsDirection();

                        // No edge tile
                        if (!tileDef.EdgeSprites.TryGetValue(direction, out var edgePath))
                            continue;

                        var texture = _resource.GetResource<TextureResource>(edgePath);
                        var box = Box2.FromDimensions(neighborIndices, tileDimensions);

                        var angle = Angle.Zero;

                        // If we ever need one for both cardinals and corners then update this.
                        switch (direction)
                        {
                            // Corner sprites
                            case Direction.SouthEast:
                                break;
                            case Direction.NorthEast:
                                angle = new Angle(MathF.PI / 2f);
                                break;
                            case Direction.NorthWest:
                                angle = new Angle(MathF.PI);
                                break;
                            case Direction.SouthWest:
                                angle = new Angle(MathF.PI * 1.5f);
                                break;
                            // Edge sprites
                            case Direction.South:
                                break;
                            case Direction.East:
                                angle = new Angle(MathF.PI / 2f);
                                break;
                            case Direction.North:
                                angle = new Angle(MathF.PI);
                                break;
                            case Direction.West:
                                angle = new Angle(MathF.PI * 1.5f);
                                break;
                        }

                        if (angle == Angle.Zero)
                            args.WorldHandle.DrawTextureRect(texture, box);
                        else
                            args.WorldHandle.DrawTextureRect(texture, new Box2Rotated(box, angle, box.Center));
                    }
                }
            }
        }

        args.WorldHandle.SetTransform(Matrix3.Identity);
    }
}
