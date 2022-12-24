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

            foreach (var tileRef in grid.GetTilesIntersecting(args.WorldBounds, false))
            {
                var tileDef = _tileDefManager[tileRef.Tile.TypeId];
                var cornerTexture = tileDef.CornerSprite != null
                    ? _resource.GetResource<TextureResource>(tileDef.CornerSprite).Texture
                    : null;
                var cardinalTexture = tileDef.CardinalSprite != null
                    ? _resource.GetResource<TextureResource>(tileDef.CardinalSprite).Texture
                    : null;

                if (cornerTexture == null && cardinalTexture == null)
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

                        switch (direction)
                        {
                            // Corner sprites
                            case Direction.SouthEast:
                                if (cornerTexture != null)
                                {
                                    var box = Box2.FromDimensions(neighborIndices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cornerTexture, box);
                                }
                                break;
                            case Direction.NorthEast:
                                if (cornerTexture != null)
                                {
                                    var box = Box2.FromDimensions(neighborIndices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cornerTexture, new Box2Rotated(box, new Angle(MathF.PI / 2f), box.Center));
                                }
                                break;
                            case Direction.NorthWest:
                                if (cornerTexture != null)
                                {
                                    var box = Box2.FromDimensions(neighborIndices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cornerTexture, new Box2Rotated(box, new Angle(MathF.PI), box.Center));
                                }
                                break;
                            case Direction.SouthWest:
                                if (cornerTexture != null)
                                {
                                    var box = Box2.FromDimensions(neighborIndices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cornerTexture, new Box2Rotated(box, new Angle(MathF.PI * 1.5f), box.Center));
                                }
                                break;
                            // Edge sprites
                            case Direction.South:
                                if (cardinalTexture != null)
                                {
                                    var box = Box2.FromDimensions(neighborIndices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cardinalTexture, box);
                                }
                                break;
                            case Direction.East:
                                if (cardinalTexture != null)
                                {
                                    var box = Box2.FromDimensions(neighborIndices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cardinalTexture, new Box2Rotated(box, new Angle(MathF.PI / 2f), box.Center));
                                }
                                break;
                            case Direction.North:
                                if (cardinalTexture != null)
                                {
                                    var box = Box2.FromDimensions(neighborIndices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cardinalTexture, new Box2Rotated(box, new Angle(MathF.PI), box.Center));
                                }
                                break;
                            case Direction.West:
                                if (cardinalTexture != null)
                                {
                                    var box = Box2.FromDimensions(neighborIndices, tileDimensions);
                                    args.WorldHandle.DrawTextureRect(cardinalTexture, new Box2Rotated(box, new Angle(MathF.PI * 1.5f), box.Center));
                                }
                                break;
                        }
                    }
                }
            }
        }

        args.WorldHandle.SetTransform(Matrix3.Identity);
    }
}
