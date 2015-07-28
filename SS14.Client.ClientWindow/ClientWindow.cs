using SS14.Shared.Maths;
using System;
using System.Drawing;

namespace SS14.Client.ClientWindow
{
    public class ClientWindowData
    {
        #region Fields

        /// <summary>
        /// Internal singleton instance holder.
        /// </summary>
        private static ClientWindowData _singleton;

        #endregion

        #region Properties

        /// <summary>
        /// Public singleton accessor property.
        /// </summary>
        public static ClientWindowData Singleton
        {
            get { return _singleton ?? (_singleton = new ClientWindowData()); }
        }

        // WorldCenter, ScreenViewportSize, and TileSize are only here because this assembly cannot access them directly.
        // Fix that when this is assembly is merged with the others.

        /// <summary>
        /// Gets the focal point of the viewport.
        /// </summary>
        public Vector2 WorldCenter { get; set; }
        //public Vector2 WorldCenter
        //{
        //    get
        //    {
        //        return PlayerManager.ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform).Position;
        //    }
        //}

        public SizeF ScreenViewportSize { get { return new SizeF(1280, 768); } set { } }
        public int TileSize { get; set; }

        /// <summary>
        /// Gets the viewport in world (tile) coordinates.
        /// </summary>
        public RectangleF WorldViewport
        {
            get
            {
                return ScreenToWorld(ScreenViewport);
            }
        }

        /// <summary>
        /// Gets the viewport in screen (pixel) coordinates.
        /// </summary>
        public RectangleF ScreenViewport
        {
            get
            {
                return new RectangleF(PointF.Empty, ScreenViewportSize);
            }
        }

        #endregion

        #region Methods

        #region Constructors

        private ClientWindowData() { }

        #endregion

        #region Publics


        /// <summary>
        /// Transforms a point from the world (tile) space, to screen (pixel) space.
        /// </summary>
        public PointF WorldToScreen(PointF point)
        {
            var center = WorldCenter;
            return new PointF(
                (point.X) * TileSize,
                (point.Y) * TileSize
                );
        }
        /// <summary>
        /// Transforms a point from the world (tile) space, to screen (pixel) space.
        /// </summary>
        public Vector2 WorldToScreen(Vector2 point)
        {
            var center = WorldCenter;
            return new Vector2(
                (point.X) * TileSize,
                (point.Y) * TileSize
                );
        }
        /// <summary>
        /// Transforms a rectangle from the world (tile) space, to screen (pixel) space.
        /// </summary>
        public RectangleF WorldToScreen(RectangleF rect)
        {
            var center = WorldCenter;
            return new RectangleF(
                (rect.X) * TileSize,
                (rect.Y) * TileSize,
                rect.Width * TileSize,
                rect.Height * TileSize
                );
        }

        /// <summary>
        /// Transforms a point from the screen (pixel) space, to world (tile) space.
        /// </summary>
        public PointF ScreenToWorld(PointF point)
        {
            var center = WorldCenter;
            return new PointF(
                (point.X) / TileSize,
                (point.Y) / TileSize
                );
        }
        /// <summary>
        /// Transforms a point from the screen (pixel) space, to world (tile) space.
        /// </summary>
        public Vector2 ScreenToWorld(Vector2 point)
        {
            var center = WorldCenter;
            return new Vector2(
                (point.X) / TileSize,
                (point.Y) / TileSize
                );
        }
        /// <summary>
        /// Transforms a rectangle from the screen (pixel) space, to world (tile) space.
        /// </summary>
        public RectangleF ScreenToWorld(RectangleF rect)
        {
            var center = WorldCenter;
            return new RectangleF(
                (rect.X) / TileSize,
                (rect.Y) / TileSize,
                rect.Width / TileSize,
                rect.Height / TileSize
                );
        }

        /// <summary>
        /// Scales a size from pixel coordinates to tile coordinates.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public SizeF PixelToTile(SizeF size)
        {
            return new SizeF(
                size.Width / TileSize,
                size.Height / TileSize
                );
        }
        /// <summary>
        /// Scales a vector from pixel coordinates to tile coordinates.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public Vector2 PixelToTile(Vector2 vec)
        {
            return new Vector2(
                vec.X / TileSize,
                vec.Y / TileSize
                );
        }
        /// <summary>
        /// Scales a rectangle from pixel coordinates to tile coordinates.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public RectangleF PixelToTile(RectangleF rect)
        {
            return new RectangleF(
                rect.X / TileSize,
                rect.Y / TileSize,
                rect.Width / TileSize,
                rect.Height / TileSize
                );
        }

        /// <summary>
        /// Takes a point in world (tile) coordinates, and rounds it to the nearest pixel.
        /// </summary>
        public PointF GetNearestPixel(PointF worldPoint)
        {
            return new PointF(
                (float)Math.Round(worldPoint.X * TileSize) / TileSize,
                (float)Math.Round(worldPoint.Y * TileSize) / TileSize
                );
        }
        /// <summary>
        /// Takes a point in world (tile) coordinates, and rounds it to the nearest pixel.
        /// </summary>
        public Vector2 GetNearestPixel(Vector2 worldPoint)
        {
            return new Vector2(
                (float)Math.Round(worldPoint.X * TileSize) / TileSize,
                (float)Math.Round(worldPoint.Y * TileSize) / TileSize
                );
        }


        #endregion

        #endregion
    }
}