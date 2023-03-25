using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class SpriteView : Control
    {
        private SpriteSystem? _spriteSystem;

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

        [ViewVariables]
        public SpriteComponent? Sprite { get; set; }

        /// <summary>
        /// Should the sprite's offset be applied to the control.
        /// </summary>
        public bool SpriteOffset = true;

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

            var uid = Sprite.Owner;

            _spriteSystem ??= IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>();
            _spriteSystem?.ForceUpdate(uid);

            renderHandle.DrawEntity(uid, PixelSize / 2 + PixelSize * (SpriteOffset ? Sprite.Offset : Vector2.Zero), Scale * UIScale, OverrideDirection);
        }
    }
}
