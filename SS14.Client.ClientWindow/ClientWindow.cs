using GorgonLibrary;
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

        /// <summary>
        /// The top left point of the screen in world coordinates.
        /// </summary>
        public Vector2D ScreenOrigin { get; private set; }

        /// <summary>
        /// Rectangle representing the viewable area in world coordinates
        /// </summary>
        public RectangleF ViewPort { get; private set; }

        /// <summary>
        /// Translates a world position to a screen position.
        /// </summary>
        /// <param name="position">position to translate</param>
        public static Vector2D WorldToScreen(Vector2D position)
        {
            return position - Singleton.ScreenOrigin;
        }

        /// <summary>
        /// Translates a screen position to a world position
        /// </summary>
        /// <param name="position">position to translate</param>
        public static Vector2D ScreenToWorld(Vector2D position)
        {
            return position + Singleton.ScreenOrigin;
        }

        #endregion

        #region Methods

        #region Constructors

        private ClientWindowData()
        {
            ScreenOrigin = new Point(0, 0);
            ViewPort = new RectangleF(ScreenOrigin, new Size(0, 0));
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
            ScreenOrigin = new Vector2D
                (
                x - Gorgon.CurrentClippingViewport.Width/2.0f,
                y - Gorgon.CurrentClippingViewport.Height/2.0f
                );
            ViewPort = new RectangleF(ScreenOrigin,
                                      new SizeF(Gorgon.CurrentClippingViewport.Width,
                                                Gorgon.CurrentClippingViewport.Height));
        }

        /// <summary>
        /// Updates the ScreenTopLeft and Viewport variables given a center point.
        /// </summary>
        /// <param name="center">Center point</param>
        public void UpdateViewPort(Vector2D center)
        {
            UpdateViewPort(center.X, center.Y);
        }

        #endregion

        #endregion
    }
}