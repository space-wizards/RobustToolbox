using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
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
            var sce = pManager.CurrentPlacementOverlayEntity;
            if (sce == null || (!IoCManager.Resolve<IEntityManager>().EntityExists(sce.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(sce.Uid).EntityLifeStage) >= EntityLifeStage.Deleted)
                return;
            var sc = IoCManager.Resolve<IEntityManager>().GetComponent<SpriteComponent>(sce.Uid);

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
            foreach (var coordinate in locationcollection)
            {
                if (!coordinate.IsValid(pManager.EntityManager))
                    return; // Just some paranoia just in case
                var entity = coordinate.GetEntity(pManager.EntityManager);
                var worldPos = coordinate.ToMapPos(pManager.EntityManager);
                var worldRot = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(entity.Uid).WorldRotation + dirAng;

                sc.Color = IsValidPosition(coordinate) ? ValidPlaceColor : InvalidPlaceColor;
                sc.Render(handle, pManager.eyeManager.CurrentEye.Rotation, worldRot, worldPos);
            }
        }

        public IEnumerable<EntityCoordinates> SingleCoordinate()
        {
            yield return MouseCoords;
        }

        public IEnumerable<EntityCoordinates> LineCoordinates()
        {
            var (x, y) = MouseCoords.ToMapPos(pManager.EntityManager) - pManager.StartPoint.ToMapPos(pManager.EntityManager);
            float iterations;
            Vector2 distance;
            if (Math.Abs(x) > Math.Abs(y))
            {
                iterations = Math.Abs(x / GridDistancing);
                distance = new Vector2(x > 0 ? 1 : -1, 0) * GridDistancing;
            }
            else
            {
                iterations = Math.Abs(y / GridDistancing);
                distance = new Vector2(0, y > 0 ? 1 : -1) * GridDistancing;
            }

            for (var i = 0; i <= iterations; i++)
            {
                yield return new EntityCoordinates(pManager.StartPoint.EntityId, pManager.StartPoint.Position + distance * i);
            }
        }

        // This name is a nice reminder of our origins. Never forget.
        public IEnumerable<EntityCoordinates> GridCoordinates()
        {
            var placementdiff = MouseCoords.ToMapPos(pManager.EntityManager) - pManager.StartPoint.ToMapPos(pManager.EntityManager);
            var distanceX = new Vector2(placementdiff.X > 0 ? 1 : -1, 0) * GridDistancing;
            var distanceY = new Vector2(0, placementdiff.Y > 0 ? 1 : -1) * GridDistancing;

            var iterationsX = Math.Abs(placementdiff.X / GridDistancing);
            var iterationsY = Math.Abs(placementdiff.Y / GridDistancing);

            for (var x = 0; x <= iterationsX; x++)
            {
                for (var y = 0; y <= iterationsY; y++)
                {
                    yield return new EntityCoordinates(pManager.StartPoint.EntityId, pManager.StartPoint.Position + distanceX * x + distanceY * y);
                }
            }
        }

        /// <summary>
        ///     Returns the tile ref for a grid, or a map.
        /// </summary>
        public TileRef GetTileRef(EntityCoordinates coordinates)
        {
            var mapCoords = coordinates.ToMap(pManager.EntityManager);
            var gridId = coordinates.GetGridId(pManager.EntityManager);
            return gridId.IsValid() ? pManager.MapManager.GetGrid(gridId).GetTileRef(MouseCoords)
                : new TileRef(mapCoords.MapId, gridId,
                    MouseCoords.ToVector2i(pManager.EntityManager, pManager.MapManager), Tile.Empty);
        }

        public TextureResource GetSprite(string key)
        {
            return pManager.ResourceCache.GetResource<TextureResource>(new ResourcePath("/Textures/") / key);
        }

        public bool TryGetSprite(string key, [NotNullWhen(true)] out TextureResource? sprite)
        {
            return pManager.ResourceCache.TryGetResource(new ResourcePath(@"/Textures/") / key, out sprite);
        }

        /// <summary>
        /// Checks if the player is spawning within a certain range of his character if range is required on this mode
        /// </summary>
        /// <returns></returns>
        public bool RangeCheck(EntityCoordinates coordinates)
        {
            if (!RangeRequired)
                return true;

            if (pManager.PlayerManager.LocalPlayer?.ControlledEntity == null)
            {
                return false;
            }

            var range = pManager.CurrentPermission!.Range;
            if (range > 0 && !IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(pManager.PlayerManager.LocalPlayer.ControlledEntity.Uid).Coordinates.InRange(pManager.EntityManager, coordinates, range))
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
            return pManager.eyeManager.ScreenToMap(point).Position;
        }

        protected Vector2 WorldToScreen(Vector2 point)
        {
            return pManager.eyeManager.WorldToScreen(point);
        }

        protected EntityCoordinates ScreenToCursorGrid(ScreenCoordinates coords)
        {
            var mapCoords = pManager.eyeManager.ScreenToMap(coords.Position);
            if (!pManager.MapManager.TryFindGridAt(mapCoords, out var grid))
            {
                return EntityCoordinates.FromMap(pManager.MapManager, mapCoords);
            }

            return EntityCoordinates.FromMap(pManager.EntityManager, grid.GridEntityId, mapCoords);
        }
    }
}
