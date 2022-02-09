using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class SpriteView : Control
    {
        private Vector2 _scale = (1, 1);

        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                InvalidateMeasure();
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

        protected override Vector2 MeasureOverride(Vector2 availableSize)
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

            EntitySystem.Get<SpriteSystem>().ForceUpdate(Sprite);
            renderHandle.DrawEntity(Sprite.Owner, PixelSize / 2, Scale * UIScale, OverrideDirection);
        }
    }
}
