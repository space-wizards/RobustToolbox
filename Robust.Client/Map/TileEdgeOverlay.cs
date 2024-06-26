using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Map;

/// <summary>
/// Draws border sprites for tiles that support them.
/// </summary>
public sealed class TileEdgeOverlay : GridOverlay
{
    private readonly IEntityManager _entManager;
    private readonly IResourceCache _resource;
    private readonly ITileDefinitionManager _tileDefManager;

    public TileEdgeOverlay(IEntityManager entManager, IResourceCache resource, ITileDefinitionManager tileDefManager)
    {
        _entManager = entManager;
        _resource = resource;
        _tileDefManager = tileDefManager;
        ZIndex = -1;
    }

    protected internal override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        var mapSystem = _entManager.System<SharedMapSystem>();
        var xformSystem = _entManager.System<SharedTransformSystem>();

        var tileSize = Grid.Comp.TileSize;
        var tileDimensions = new Vector2(tileSize, tileSize);
        var (_, _, worldMatrix, invMatrix) = xformSystem.GetWorldPositionRotationMatrixWithInv(Grid.Owner);
        args.WorldHandle.SetTransform(worldMatrix);
        var bounds = args.WorldBounds;
        bounds = new Box2Rotated(bounds.Box.Enlarged(1), bounds.Rotation, bounds.Origin);
        var localAABB = invMatrix.TransformBox(bounds);

        var enumerator = mapSystem.GetLocalTilesEnumerator(Grid.Owner, Grid, localAABB, false);

        while (enumerator.MoveNext(out var tileRef))
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
                    var neighborTile = mapSystem.GetTileRef(Grid.Owner, Grid, neighborIndices);
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
                        args.WorldHandle.DrawTextureRect(texture.Texture, box);
                    else
                        args.WorldHandle.DrawTextureRect(texture.Texture, new Box2Rotated(box, angle, box.Center));

                    RequiresFlush = true;
                }
            }
        }

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }
}
