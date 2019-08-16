using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public class SpriteView : Control
    {
        private Vector2 _scale = (1, 1);

        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                MinimumSizeChanged();
            }
        }

        public ISpriteComponent Sprite { get; set; }

        public SpriteView()
        {
            RectClipContent = true;
        }

        protected override Vector2 CalculateMinimumSize()
        {
            // TODO: make this not hardcoded.
            // It'll break on larger things.
            return (32, 32) * Scale;
        }

        internal override void DrawInternal(IRenderHandle renderHandle)
        {
            if (Sprite == null)
            {
                return;
            }

            renderHandle.DrawEntity(Sprite.Owner, GlobalPixelPosition + PixelSize / 2, Scale);
        }
    }
}
