using SS14.Shared;
using System.Drawing;
using SS14.Client.Graphics.CluwneLib;
using SS14.Shared.Maths;

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

        /// <summary>
        /// The top left point of the screen in world coordinates.
        /// </summary>
        public Vector2 ScreenOrigin { get; private set; }

        /// <summary>
        /// Rectangle representing the viewable area in world coordinates
        /// </summary>
        public RectangleF ViewPort { get; private set; }

        /// <summary>
        /// Translates a world position to a screen position.
        /// </summary>
        /// <param name="position">position to translate</param>
        public static Vector2 WorldToScreen(Vector2 position)
        {
            return position - Singleton.ScreenOrigin;
        }

        /// <summary>
        /// Translates a screen position to a world position
        /// </summary>
        /// <param name="position">position to translate</param>
        public static Vector2 ScreenToWorld(Vector2 position)
        {
            return position + Singleton.ScreenOrigin;
        }

        #endregion

        #region Methods

        #region Constructors

        private ClientWindowData()
        {
            ScreenOrigin = new Vector2(0, 0);
            ViewPort = new RectangleF(new PointF(ScreenOrigin.X, ScreenOrigin.Y), new Size(0, 0));
        }

        #endregion

        #region Publics

        /// <summary>
        /// Updates the ScreenTopLeft and Viewport variables given a center point.
        /// WORLD POSITION
        /// </summary>
        /// <param name="x">Center point x</param>
        /// <param name="y">Center point y</param>
        public void UpdateViewPort(float x, float y)
        {
            ScreenOrigin = new Vector2
                (
                x - CluwneLib.CurrentClippingViewport.Width/2.0f,
                y - CluwneLib.CurrentClippingViewport.Height/2.0f
                );
            ViewPort = new RectangleF(ScreenOrigin,
                                      new SizeF(CluwneLib.CurrentClippingViewport.Width,
                                                CluwneLib.CurrentClippingViewport.Height));
        }

        /// <summary>
        /// Updates the ScreenTopLeft and Viewport variables given a center point.
        /// </summary>
        /// <param name="center">Center point</param>
        public void UpdateViewPort(Vector2 center)
        {
            UpdateViewPort(center.X, center.Y);
        }

        #endregion

        #endregion
    }
}