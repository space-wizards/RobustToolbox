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
            //Add the Sprite.Offset and if SpriteOffset = false, adjust the SpriteOffset center point if the sprite is > 32px.
            var  offsetAdj = (SpriteOffset ? Sprite.Offset : new Vector2(0,(1.0f/(32*Sprite.Bounds.Height))*32));

            _spriteSystem ??= IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>();
            _spriteSystem?.ForceUpdate(uid);

            renderHandle.DrawEntity(uid, PixelSize / 2 + PixelSize * offsetAdj, Scale * UIScale, OverrideDirection);
        }
    }
}
