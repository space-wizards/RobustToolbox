using System;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
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

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        foreach (var grid in _mapManager.FindGridsIntersecting(args.MapId, args.WorldBounds))
        {
            var tileSize = grid.TileSize;
            var tileDimensions = new Vector2(tileSize, tileSize);
            var xform = xformQuery.GetComponent(grid.Owner);
            args.WorldHandle.SetTransform(xform.WorldMatrix);

            // TODO: Add support for edge variants
            // TODO: List of edges supported for the thing
            // Then, each tile needs to store its cardinal / corner variants.

            foreach (var tileRef in grid.GetTilesIntersecting(args.WorldBounds, false))
            {
                var tileDef = _tileDefManager[tileRef.Tile.TypeId];

                if (tileDef.CardinalSprites.Count == 0 && tileDef.CornerSprites.Count == 0)
                    continue;

                // Get what tiles border us to determine what sprites we need to draw.
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        var neighborIndices = new Vector2i(tileRef.GridIndices.X + x, tileRef.GridIndices.Y + y);
                        var neighborTile = grid.GetTileRef(neighborIndices);

                        // If it's the same tile then no edge to be drawn.
                        if (tileRef.Tile.TypeId == neighborTile.Tile.TypeId)
                            continue;

                        var direction = new Vector2i(x, y).AsDirection();
                        var intDirection = (int)direction;
                        var box = Box2.FromDimensions(neighborIndices, tileDimensions);
                        var variants = tileDef.CornerSprites.Count;
                        var variant = (tileRef.GridIndices.X + tileRef.GridIndices.Y * 4 + intDirection) % variants;

                        Angle angle = Angle.Zero;
                        Texture? texture = null;

                        switch (direction)
                        {
                            // Corner sprites
                            case Direction.SouthEast:
                                if (tileDef.CornerSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CornerSprites[variant])
                                        .Texture;
                                }
                                break;
                            case Direction.NorthEast:
                                if (tileDef.CornerSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CornerSprites[variant])
                                        .Texture;

                                    angle = new Angle(MathF.PI / 2f);
                                }
                                break;
                            case Direction.NorthWest:
                                if (tileDef.CornerSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CornerSprites[variant])
                                        .Texture;

                                    angle = new Angle(MathF.PI);
                                }
                                break;
                            case Direction.SouthWest:
                                if (tileDef.CornerSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CornerSprites[variant])
                                        .Texture;

                                    angle = new Angle(MathF.PI * 1.5f);
                                }
                                break;
                            // Edge sprites
                            case Direction.South:
                                if (tileDef.CardinalSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CardinalSprites[variant])
                                        .Texture;
                                }
                                break;
                            case Direction.East:
                                if (tileDef.CardinalSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CardinalSprites[variant])
                                        .Texture;

                                    angle = new Angle(MathF.PI / 2f);
                                }
                                break;
                            case Direction.North:
                                if (tileDef.CardinalSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CardinalSprites[variant])
                                        .Texture;

                                    angle = new Angle(MathF.PI);
                                }
                                break;
                            case Direction.West:
                                if (tileDef.CardinalSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CardinalSprites[variant])
                                        .Texture;

                                    angle = new Angle(MathF.PI * 1.5f);
                                }
                                break;
                        }

                        if (texture == null)
                            continue;

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
