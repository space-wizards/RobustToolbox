using SFML.Graphics;
using SFML.Window;
using System;


namespace SS14.Client.Graphics.Render
{
    public class CluwneWindow : RenderWindow
    {
        public CluwneWindow(VideoMode mode, string title) : base(mode, title)
        {
           
            

        }

        public CluwneWindow(VideoMode mode, string title, Styles style) : base(mode, title, style)
        {
        }

        public CluwneWindow(VideoMode mode, string title, Styles style, ContextSettings settings) : base(mode, title, style, settings)
        {
        }

        public CluwneWindow(IntPtr handle) : base(handle)
        {
        }

        public CluwneWindow(IntPtr handle, ContextSettings settings) : base(handle, settings)
        {
        }

        public Color BackgroundColor
        {
            get;
            set;
        }
    }
}
