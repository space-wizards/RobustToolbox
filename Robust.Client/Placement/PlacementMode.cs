using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Robust.Client.Placement
{
    public abstract class PlacementMode
    {
        public readonly PlacementManager pManager;

        /// <summary>
        /// Holds the current tile we are hovering our mouse over
        /// </summary>
        public TileRef CurrentTile { get; set; }

        /// <summary>
        /// Local coordinates of our cursor on the map
        /// </summary>
        public EntityCoordinates MouseCoords { get; set; }

        /// <summary>
        /// Texture resources to draw to represent the entity we are trying to spawn
        /// </summary>
        public List<Texture>? TexturesToDraw { get; set; }

        /// <summary>
        /// Color set to the ghost entity when it has a valid spawn position
        /// </summary>
        public Color ValidPlaceColor { get; set; } = new(20, 180, 20); //Default valid color is green

        /// <summary>
        /// Color set to the ghost entity when it has an invalid spawn position
        /// </summary>
        public Color InvalidPlaceColor { get; set; } = new(180, 20, 20); //Default invalid placement is red

        /// <summary>
        /// Used for line and grid placement to determine how spaced apart the entities should be
        /// </summary>
        protected float GridDistancing = 1f;

        /// <summary>
        /// Whether this mode requires us to verify the player is spawning within a certain range of themselves
        /// </summary>
        public virtual bool RangeRequired => false;

        /// <summary>
        /// Whether this mode can use the line placement mode
        /// </summary>
        public virtual bool HasLineMode => false;

        /// <summary>
        /// Whether this mode can use the grid placement mode
        /// </summary>
        public virtual bool HasGridMode => false;

        protected PlacementMode(PlacementManager pMan)
        {
            pManager = pMan;
        }

        public virtual string ModeName => GetType().Name;

        /// <summary>
        /// Aligns the location of placement based on cursor location
        /// </summary>
        /// <param name="mouseScreen"></param>
        /// <returns>Returns whether the current position is a valid placement position</returns>
        public abstract void AlignPlacementMode(ScreenCoordinates mouseScreen);

        /// <summary>
        /// Verifies the location of placement is a valid position to place at
        /// </summary>
        /// <param name="mouseScreen"></param>
        /// <returns></returns>
        public abstract bool IsValidPosition(EntityCoordinates position);

        public virtual void Render(DrawingHandleWorld handle)
        {
            var uid = pManager.CurrentPlacementOverlayEntity;
            if (!pManager.EntityManager.TryGetComponent(uid, out SpriteComponent? sprite) || !sprite.Visible)
            {
                // TODO draw something for placement of invisible & sprite-less entities.
                return;
            }

            IEnumerable<EntityCoordinates> locationcollection;
            switch (pManager.PlacementType)
            {
                case PlacementManager.PlacementTypes.None:
                    locationcollection = SingleCoordinate();
                    break;
                case PlacementManager.PlacementTypes.Line:
                    locationcollection = LineCoordinates();
                    break;
                case PlacementManager.PlacementTypes.Grid:
                    locationcollection = GridCoordinates();
                    break;
                default:
                    locationcollection = SingleCoordinate();
                    break;
            }

            var dirAng = pManager.Direction.ToAngle();
            var spriteSys = pManager.EntityManager.System<SpriteSystem>();
            foreach (var coordinate in locationcollection)
            {
                if (!coordinate.IsValid(pManager.EntityManager))
                    return; // Just some paranoia just in case
                var worldPos = coordinate.ToMapPos(pManager.EntityManager);
                var worldRot = pManager.EntityManager.GetComponent<TransformComponent>(coordinate.EntityId).WorldRotation + dirAng;

                sprite.Color = IsValidPosition(coordinate) ? ValidPlaceColor : InvalidPlaceColor;
                spriteSys.Render(uid.Value, sprite, handle, pManager.EyeManager.CurrentEye.Rotation, worldRot, worldPos);
            }
        }

        public IEnumerable<EntityCoordinates> SingleCoordinate()
        {
            yield return MouseCoords;
        }

        public IEnumerable<EntityCoordinates> LineCoordinates()
        {
            var mouseScreen = pManager.InputManager.MouseScreenPosition;
            var mousePos = pManager.EyeManager.ScreenToMap(mouseScreen);

            if (mousePos.MapId == MapId.Nullspace)
                yield break;

            var (_, (x, y)) = EntityCoordinates.FromMap(pManager.StartPoint.EntityId, mousePos, pManager.EntityManager) - pManager.StartPoint;
            float iterations;
            Vector2 distance;
            if (Math.Abs(x) > Math.Abs(y))
            {
                var xSign = Math.Sign(x);
                iterations = MathF.Ceiling(Math.Abs((x + GridDistancing / 2f * xSign) / GridDistancing));
                distance = new Vector2(x > 0 ? 1 : -1, 0);
            }
            else
            {
                var ySign = Math.Sign(y);
                iterations = MathF.Ceiling(Math.Abs((y + GridDistancing / 2f * ySign) / GridDistancing));
                distance = new Vector2(0, y > 0 ? 1 : -1);
            }

            for (var i = 0; i < iterations; i++)
            {
                yield return new EntityCoordinates(pManager.StartPoint.EntityId, pManager.StartPoint.Position + distance * i);
            }
        }

        // This name is a nice reminder of our origins. Never forget.
        public IEnumerable<EntityCoordinates> GridCoordinates()
        {
            var mouseScreen = pManager.InputManager.MouseScreenPosition;
            var mousePos = pManager.EyeManager.ScreenToMap(mouseScreen);

            if (mousePos.MapId == MapId.Nullspace)
                yield break;

            var placementdiff = EntityCoordinates.FromMap(pManager.StartPoint.EntityId, mousePos, pManager.EntityManager) - pManager.StartPoint;

            var xSign = Math.Sign(placementdiff.X);
            var ySign = Math.Sign(placementdiff.Y);
            var iterationsX = Math.Ceiling(Math.Abs((placementdiff.X + GridDistancing / 2f * xSign) / GridDistancing));
            var iterationsY = Math.Ceiling(Math.Abs((placementdiff.Y + GridDistancing / 2f * ySign) / GridDistancing));

            for (var x = 0; x < iterationsX; x++)
            {
                for (var y = 0; y < iterationsY; y++)
                {
                    yield return new EntityCoordinates(pManager.StartPoint.EntityId, pManager.StartPoint.Position + new Vector2(x * xSign, y * ySign) * GridDistancing);
                }
            }
        }

        /// <summary>
        ///     Returns the tile ref for a grid, or a map.
        /// </summary>
        public TileRef GetTileRef(EntityCoordinates coordinates)
        {
            var gridUidOpt = coordinates.GetGridUid(pManager.EntityManager);
            return gridUidOpt is EntityUid gridUid && gridUid.IsValid() ? pManager.MapManager.GetGrid(gridUid).GetTileRef(MouseCoords)
                : new TileRef(gridUidOpt ?? EntityUid.Invalid,
                    MouseCoords.ToVector2i(pManager.EntityManager, pManager.MapManager), Tile.Empty);
        }

        public TextureResource GetSprite(string key)
        {
            return pManager.ResourceCache.GetResource<TextureResource>(new ResPath("/Textures/") / key);
        }

        public bool TryGetSprite(string key, [NotNullWhen(true)] out TextureResource? sprite)
        {
            return pManager.ResourceCache.TryGetResource(new ResPath(@"/Textures/") / key, out sprite);
        }

        /// <summary>
        /// Checks if the player is spawning within a certain range of his character if range is required on this mode
        /// </summary>
        /// <returns></returns>
        public bool RangeCheck(EntityCoordinates coordinates)
        {
            if (!RangeRequired)
                return true;
            var controlled = pManager.PlayerManager.LocalPlayer?.ControlledEntity ?? EntityUid.Invalid;
            if (controlled == EntityUid.Invalid)
            {
                return false;
            }

            var range = pManager.CurrentPermission!.Range;
            if (range > 0 && !pManager.EntityManager.GetComponent<TransformComponent>(controlled).Coordinates.InRange(pManager.EntityManager, coordinates, range))
                return false;
            return true;
        }

        public bool IsColliding(EntityCoordinates coordinates)
        {
            var bounds = pManager.ColliderAABB;
            var mapCoords = coordinates.ToMap(pManager.EntityManager);
            var (x, y) = mapCoords.Position;

            var collisionBox = Box2.FromDimensions(
                bounds.Left + x,
                bounds.Bottom + y,
                bounds.Width,
                bounds.Height);

            return EntitySystem.Get<SharedPhysicsSystem>().TryCollideRect(collisionBox, mapCoords.MapId);
        }

        protected Vector2 ScreenToWorld(Vector2 point)
        {
            return pManager.EyeManager.ScreenToMap(point).Position;
        }

        protected Vector2 WorldToScreen(Vector2 point)
        {
            return pManager.EyeManager.WorldToScreen(point);
        }

        protected EntityCoordinates ScreenToCursorGrid(ScreenCoordinates coords)
        {
            var mapCoords = pManager.EyeManager.ScreenToMap(coords.Position);
            if (!pManager.MapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid))
            {
                return EntityCoordinates.FromMap(pManager.MapManager, mapCoords);
            }

            return EntityCoordinates.FromMap(pManager.EntityManager, gridUid, mapCoords);
        }
    }
}
