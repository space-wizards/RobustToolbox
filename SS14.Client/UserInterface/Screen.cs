using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.UserInterface.Components;

namespace SS14.Client.UserInterface
{
    class Screen : GuiComponent
    {
        public Sprite Background { get; set; }

        public override void Render()
        {
            Background?.Draw();

            base.Render();
        }

        public override void Resize()
        {
            if(Background != null)
                Background.Scale = new Vector2f((float)Width / Background.TextureRect.Width, (float)Height / Background.TextureRect.Height);

            base.Resize();
        }
    }
}
