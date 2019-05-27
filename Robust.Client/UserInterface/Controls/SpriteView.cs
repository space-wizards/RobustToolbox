using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Log;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public class SpriteView : Control
    {
        public ISpriteComponent Sprite { get; set; }

        public SpriteView()
        {
        }

        public SpriteView(string name) : base(name)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            RectClipContent = true;
        }

        protected override Vector2 CalculateMinimumSize()
        {
            // TODO: make this not hardcoded.
            // It'll break on larger things.
            return (32, 32);
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            if (Sprite == null)
            {
                return;
            }

            handle.DrawEntity(Sprite.Owner, GlobalPixelPosition + PixelSize / 2);
        }
    }
}
