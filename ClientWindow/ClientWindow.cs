using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace ClientWindow
{
    public class ClientWindowData
    {
        /// <summary>
        /// The top left point of the screen in world coordinates.
        /// </summary>
        private Vector2D m_screenTopLeft;
        /// <summary>
        /// The top left point of the screen in world coordinates.
        /// </summary>
        public Vector2D ScreenTopLeft
        {
            get { return m_screenTopLeft; }
            private set { }
        }

        /// <summary>
        /// Rectangle representing the viewable area in world coordinates
        /// </summary>
        private RectangleF m_viewPort;
        /// <summary>
        /// Rectangle representing the viewable area in world coordinates
        /// </summary>
        public RectangleF ViewPort
        {
            get { return m_viewPort; }
            private set { }
        }
        
        private static ClientWindowData singleton;
        public static ClientWindowData Singleton
        {
            get {
                if (singleton == null)
                    singleton = new ClientWindowData();
                return singleton; 
            }
            set { }
        }

        public ClientWindowData()
        {
            m_screenTopLeft = new Point(0, 0);
            m_viewPort = new RectangleF(m_screenTopLeft, new Size(0, 0));
        }

        /// <summary>
        /// Updates the ScreenTopLeft and Viewport variables given a center point.
        /// </summary>
        /// <param name="x">Center point x</param>
        /// <param name="y">Center point y</param>
        public void UpdateViewPort(int x, int y)
        {
            m_screenTopLeft = new Vector2D(x - Gorgon.Screen.Width / 2,
                y - Gorgon.Screen.Height / 2);
            m_viewPort = new RectangleF(m_screenTopLeft, new Size(Gorgon.Screen.Width, Gorgon.Screen.Height));
        }

        public void UpdateViewPort(float x, float y)
        {
            m_screenTopLeft = new Vector2D(x - Gorgon.Screen.Width / 2,
                y - Gorgon.Screen.Height / 2);
            m_viewPort = new RectangleF(m_screenTopLeft, new Size(Gorgon.Screen.Width, Gorgon.Screen.Height));
        }

        public void UpdateViewPort(Point center)
        {
            UpdateViewPort(center.X, center.Y);
        }

        public void UpdateViewPort(Vector2D center)
        {
            UpdateViewPort(center.X, center.Y);
        }

        public static float xTopLeft
        {
            get
            {
                return Singleton.ScreenTopLeft.X;
            }
            private set { }
        }

        public static float yTopLeft
        {
            get
            {
                return Singleton.ScreenTopLeft.Y;
            }
            private set { }
        }
    }
}
