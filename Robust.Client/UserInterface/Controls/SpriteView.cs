using Robust.Client.GameObjects;
using Robust.Client.Graphics.Interfaces.Graphics;
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

        public ISpriteComponent? Sprite { get; set; }

        /// <summary>
        ///     Overrides the direction used to render the sprite.
        /// </summary>
        /// <remarks>
        ///     If null, the world space orientation of the entity will be used.
        ///     Otherwise the specified direction will be used.
        /// </remarks>
        public Direction? OverrideDirection { get; set; }

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
            if (Sprite == null || Sprite.Deleted)
            {
                return;
            }

            renderHandle.DrawEntity(Sprite.Owner, GlobalPixelPosition + PixelSize / 2, Scale, OverrideDirection);
        }
    }
}
