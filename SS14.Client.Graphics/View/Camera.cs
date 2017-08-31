using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using SFML.Graphics;

namespace SS14.Client.Graphics.View
{
    public class Camera
    {
        private Viewport _view;
        private RenderWindow _viewport;

        public Camera(Viewport viewport)
        {
            
        }

        public Camera(RenderWindow viewport)
        {
            _viewport = viewport;
        }

        public Vector2 Position { get; set; }

        public void SetView(SFML.Graphics.View view)
        {
            _viewport.SetView(view);
        }
    }
}
