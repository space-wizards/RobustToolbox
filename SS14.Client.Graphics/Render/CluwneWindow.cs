using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using SFML.Window;
using System.Drawing;
using Color = System.Drawing.Color;


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

        public System.Drawing.Color BackgroundColor
        {
            get;
            set;
        }
        
        public void Clear(Color color)
        {
            base.Clear(color.ToSFMLColor());
        }
     
    }
}
