using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Collections;
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

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public TileEdgeOverlay(IEntityManager entManager, IMapManager mapManager, IResourceCache resource, ITileDefinitionManager tileDefManager)
    {
        _entManager = entManager;
        _mapManager = mapManager;
        _resource = resource;
        _tileDefManager = tileDefManager;
    }

    protected internal override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        var neighborGroups = new Dictionary<ushort, ValueList<Direction>>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        foreach (var grid in _mapManager.FindGridsIntersecting(args.MapId, args.WorldBounds))
        {
            var tileSize = grid.TileSize;
            var xform = xformQuery.GetComponent(grid.Owner);
            args.WorldHandle.SetTransform(xform.WorldMatrix);

            foreach (var tileRef in grid.GetTilesIntersecting(args.WorldBounds, false))
            {
                // Get what tiles border us to determine what sprites we need to draw.
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        var neighborIndices = new Vector2i(tileRef.GridIndices.X + x, tileRef.GridIndices.Y + y);
                        var neighborTile = grid.GetTileRef(neighborIndices);

                        // If it's the same tile then no edge to be drawn.
                        if (tileRef.Tile.TypeId == neighborTile.Tile.TypeId)
                            continue;

                        var direction = new Vector2i(x, y).AsDirection();

                        // If the neighboring tile doesn't support edges then ignore it.
                        switch (direction)
                        {
                            case Direction.West:
                            case Direction.North:
                            case Direction.East:
                            case Direction.South:
                                if (_tileDefManager[neighborTile.Tile.TypeId].CardinalSprite == null)
                                    continue;
                                break;
                            case Direction.SouthEast:
                            case Direction.NorthEast:
                            case Direction.NorthWest:
                            case Direction.SouthWest:
                                if (_tileDefManager[neighborTile.Tile.TypeId].CornerSprite == null)
                                    continue;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        if (!neighborGroups.TryGetValue(neighborTile.Tile.TypeId, out var directions))
                        {
                            directions = new ValueList<Direction>();
                        }

                        directions.Add(direction);
                        neighborGroups[neighborTile.Tile.TypeId] = directions;
                    }
                }

                if (neighborGroups.Count == 0)
                    continue;

                foreach (var (neighborGroup, flags) in neighborGroups)
                {
                    var tile = _tileDefManager[neighborGroup];
                    var cornerTexture = tile.CornerSprite != null
                        ? _resource.GetResource<TextureResource>(tile.CornerSprite).Texture
                        : null;
                    var cardinalTexture = tile.CardinalSprite != null
                        ? _resource.GetResource<TextureResource>(tile.CardinalSprite).Texture
                        : null;
                    var indices = (Vector2)tileRef.GridIndices;
                    var tileDimensions = (Vector2)(tileSize, tileSize);

                    foreach (var dir in flags)
                    {
                        switch (dir)
                        {
                            // Corner sprites
                            case Direction.NorthWest:
                                if (cornerTexture != null &&
                                    !flags.Contains(Direction.West) &&
                                    !flags.Contains(Direction.North))
                                {
                                    var box = Box2.FromDimensions(indices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cornerTexture, box);
                                }
                                break;
                            case Direction.SouthWest:
                                if (cornerTexture != null &&
                                    !flags.Contains(Direction.West) &&
                                    !flags.Contains(Direction.South))
                                {
                                    var box = Box2.FromDimensions(indices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cornerTexture, new Box2Rotated(box, new Angle(MathF.PI / 2f), box.Center));
                                }
                                break;
                            case Direction.SouthEast:
                                if (cornerTexture != null &&
                                    !flags.Contains(Direction.East) &&
                                    !flags.Contains(Direction.South))
                                {
                                    var box = Box2.FromDimensions(indices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cornerTexture, new Box2Rotated(box, new Angle(MathF.PI), box.Center));
                                }
                                break;
                            case Direction.NorthEast:
                                if (cornerTexture != null &&
                                    !flags.Contains(Direction.East) &&
                                    !flags.Contains(Direction.North))
                                {
                                    var box = Box2.FromDimensions(indices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cornerTexture, new Box2Rotated(box, new Angle(MathF.PI * 1.5f), box.Center));
                                }
                                break;
                            // Edge sprites
                            case Direction.North:
                                if (cardinalTexture != null)
                                {
                                    var box = Box2.FromDimensions(indices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cardinalTexture, box);
                                }
                                break;
                            case Direction.West:
                                if (cardinalTexture != null)
                                {
                                    var box = Box2.FromDimensions(indices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cardinalTexture, new Box2Rotated(box, new Angle(MathF.PI / 2f), box.Center));
                                }
                                break;
                            case Direction.South:
                                if (cardinalTexture != null)
                                {
                                    var box = Box2.FromDimensions(indices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cardinalTexture, new Box2Rotated(box, new Angle(MathF.PI), box.Center));
                                }
                                break;
                            case Direction.East:
                                if (cardinalTexture != null)
                                {
                                    var box = Box2.FromDimensions(indices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cardinalTexture, new Box2Rotated(box, new Angle(MathF.PI * 1.5f), box.Center));
                                }
                                break;
                        }
                    }

                    flags.Clear();
                }

                neighborGroups.Clear();
            }
        }

        args.WorldHandle.SetTransform(Matrix3.Identity);
    }
}
