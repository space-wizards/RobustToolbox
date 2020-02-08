using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Drawing;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Placement
{
    abstract public class PlacementMode
    {
        public readonly PlacementManager pManager;

        /// <summary>
        /// Holds the current tile we are hovering our mouse over
        /// </summary>
        public TileRef CurrentTile { get; set; }

        /// <summary>
        /// Local coordinates of our cursor on the map
        /// </summary>
        public GridCoordinates MouseCoords { get; set; }

        /// <summary>
        /// Texture resource to draw to represent the entity we are tryign to spawn
        /// </summary>
        public Texture SpriteToDraw { get; set; }

        /// <summary>
        /// Color set to the ghost entity when it has a valid spawn position
        /// </summary>
        public Color ValidPlaceColor { get; set; } = new Color(20, 180, 20); //Default valid color is green

        /// <summary>
        /// Color set to the ghost entity when it has an invalid spawn position
        /// </summary>
        public Color InvalidPlaceColor { get; set; } = new Color(180, 20, 20); //Default invalid placement is red

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
        public abstract bool IsValidPosition(GridCoordinates position);

        public virtual void Render(DrawingHandleWorld handle)
        {
            if (SpriteToDraw == null)
            {
                SetSprite();
            }

            IEnumerable<GridCoordinates> locationcollection;
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

            var size = SpriteToDraw.Size;
            foreach (var coordinate in locationcollection)
            {
                var worldPos = pManager.MapManager.GetGrid(coordinate.GridID).LocalToWorld(coordinate).Position;
                var pos = worldPos - (size/(float)EyeManager.PIXELSPERMETER) / 2f;
                var color = IsValidPosition(coordinate) ? ValidPlaceColor : InvalidPlaceColor;
                handle.DrawTexture(SpriteToDraw, pos, color);
            }
        }

        public IEnumerable<GridCoordinates> SingleCoordinate()
        {
            yield return MouseCoords;
        }

        public IEnumerable<GridCoordinates> LineCoordinates()
        {
            var (x, y) = MouseCoords.ToMapPos(pManager.MapManager) - pManager.StartPoint.ToMapPos(pManager.MapManager);
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
                yield return new GridCoordinates(pManager.StartPoint.Position + distance * i, pManager.StartPoint.GridID);
            }
        }

        public IEnumerable<GridCoordinates> GridCoordinates()
        {
            var placementdiff = MouseCoords.ToMapPos(pManager.MapManager) - pManager.StartPoint.ToMapPos(pManager.MapManager);
            var distanceX = new Vector2(placementdiff.X > 0 ? 1 : -1, 0) * GridDistancing;
            var distanceY = new Vector2(0, placementdiff.Y > 0 ? 1 : -1) * GridDistancing;

            var iterationsX = Math.Abs(placementdiff.X / GridDistancing);
            var iterationsY = Math.Abs(placementdiff.Y / GridDistancing);

            for (var x = 0; x <= iterationsX; x++)
            {
                for (var y = 0; y <= iterationsY; y++)
                {
                    yield return new GridCoordinates(pManager.StartPoint.Position + distanceX * x + distanceY * y, pManager.StartPoint.GridID);
                }
            }
        }

        public TextureResource GetSprite(string key)
        {
            return pManager.ResourceCache.GetResource<TextureResource>(new ResourcePath("/Textures/") / key);
        }

        public bool TryGetSprite(string key, out TextureResource sprite)
        {
            return pManager.ResourceCache.TryGetResource(new ResourcePath(@"/Textures/") / key, out sprite);
        }

        public void SetSprite()
        {
            SpriteToDraw = pManager.CurrentBaseSprite.TextureFor(pManager.Direction);
        }

        /// <summary>
        /// Checks if the player is spawning within a certain range of his character if range is required on this mode
        /// </summary>
        /// <returns></returns>
        public bool RangeCheck(GridCoordinates coordinates)
        {
            if (!RangeRequired)
                return true;
            var range = pManager.CurrentPermission.Range;
            if (range > 0 && !pManager.PlayerManager.LocalPlayer.ControlledEntity.Transform.GridPosition.InRange(pManager.MapManager, coordinates, range))
                return false;
            return true;
        }

        public bool IsColliding(GridCoordinates coordinates)
        {
            var bounds = pManager.ColliderAABB;
            var worldcoords = coordinates.ToMapPos(pManager.MapManager);

            var collisionbox = Box2.FromDimensions(
                bounds.Left + worldcoords.X,
                bounds.Bottom + worldcoords.Y,
                bounds.Width,
                bounds.Height);

            if (pManager.PhysicsManager.IsColliding(collisionbox, pManager.MapManager.GetGrid(coordinates.GridID).ParentMapId))
                return true;

            return false;
        }

        protected Vector2 ScreenToWorld(Vector2 point)
        {
            return pManager.eyeManager.ScreenToMap(point).Position;
        }

        protected Vector2 WorldToScreen(Vector2 point)
        {
            return pManager.eyeManager.WorldToScreen(point);
        }

        protected GridCoordinates ScreenToCursorGrid(ScreenCoordinates coords)
        {
            return pManager.eyeManager.ScreenToWorld(coords.Position);
        }
    }
}
