using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.UserInterface.Components;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface
{
    public class Screen : GuiComponent
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

            _clientArea = Box2i.FromDimensions(Position.X, Position.Y, Width, Height);

            base.Resize();
        }
    }
}
