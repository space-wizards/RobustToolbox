using System;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class SpriteView : Control
    {
        private SpriteSystem? _spriteSystem;
        IEntityManager _entMan;

        [ViewVariables]
        private SpriteComponent? _sprite;
        public SpriteComponent? Sprite
        {
            get => _sprite;
            [Obsolete("Use SetEntity()")]
            set => SetEntity(value?.Owner);
        }


        [ViewVariables]
        public EntityUid? Entity { get; private set; }

        /// <summary>
        /// If true this will scale the sprite up or down to fit within the control's actual size.
        /// </summary>
        public StretchMode Stretch  { get; set; } = StretchMode.Fit;

        public enum StretchMode
        {
            /// <summary>
            /// Don't scale the sprite at all.
            /// </summary>
            None,

            /// <summary>
            /// Scales the sprite down so that it fits within the control. Does not scale the sprite up.
            /// </summary>
            Fit,

            /// <summary>
            ///  Scale the sprite so that it fills the whole control.
            /// </summary>
            Fill
        }

        /// <summary>
        /// Overrides the direction used to render the sprite.
        /// </summary>
        /// <remarks>
        /// If null, the world space orientation of the entity will be used. Otherwise the specified direction will be
        /// used.
        /// </remarks>
        public Direction? OverrideDirection { get; set; }

        #region Transform

        private Vector2 _scale = Vector2.One;
        private Angle _eyeRotation = Angle.Zero;
        private Angle? _worldRotation = Angle.Zero;

        public Angle EyeRotation
        {
            get => _eyeRotation;
            set
            {
                _eyeRotation = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Used to override the entity's world rotation. Note that the desired size of the control will not
        /// automatically get updated as the entity's world rotation changes.
        /// </summary>
        public Angle? WorldRotation
        {
            get => _worldRotation;
            set
            {
                _worldRotation = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Scale to apply when rendering the sprite. This is separate from the sprite's scale.
        /// </summary>
        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Cached desired size. Differs from <see cref="Control.DesiredSize"/> as it it is not clamped by the
        /// minimum/maximum size options.
        /// </summary>
        private Vector2 _spriteSize;

        /// <summary>
        /// Determines whether or not the sprite's offset be applied to the control.
        /// </summary>
        public bool SpriteOffset { get; set; }

        #endregion

        public SpriteView()
        {
            _entMan = IoCManager.Resolve<IEntityManager>();
            _entMan.TryGetComponent(Entity, out _sprite);
            RectClipContent = true;
        }

        public void SetEntity(EntityUid? uid)
        {
            Entity = uid;
            _entMan.TryGetComponent(Entity, out _sprite);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            // TODO Make this get called when sprite bounds/properties update?
            UpdateSize();
            return _spriteSize;
        }

        private void UpdateSize()
        {
            if (Entity == null || _sprite == null)
            {
                _spriteSize = default;
                return;
            }

            var spriteBox = _sprite.CalculateRotatedBoundingBox(default,  _worldRotation ?? Angle.Zero, _eyeRotation)
                .CalcBoundingBox();

            if (!SpriteOffset)
            {
                // re-center the box.
                spriteBox = spriteBox.Translated(-spriteBox.Center);
            }

            // Scale the box (including any offset);
            var scale = _scale * EyeManager.PixelsPerMeter;
            var bl = spriteBox.BottomLeft * scale;
            var tr = spriteBox.TopRight * scale;

            // This view will be centered on (0,0). If the sprite was shifted by (1,2) the actual size of the control
            // would need to be at least (2,4).
            tr = Vector2.ComponentMax(tr, Vector2.Zero);
            bl = Vector2.ComponentMin(bl, Vector2.Zero);
            tr = Vector2.ComponentMax(tr, -bl);
            bl = Vector2.ComponentMin(bl, -tr);
            var box = new Box2(bl, tr);

            DebugTools.Assert(box.Contains(Vector2.Zero));
            DebugTools.Assert(box.TopLeft.EqualsApprox(-box.BottomRight));

            if (_worldRotation != null
                && _eyeRotation == Angle.Zero) // TODO This shouldn't need to be here, but I just give up at this point I am going fucking insane looking at rotating blobs of pixels. I doubt anyone will ever even use rotated sprite views.?
            {
                _spriteSize = box.Size;
                return;
            }

            // Size does not auto-update with world rotation. So if it is not fixed by _worldRotation we will just take
            // the maximum possible size.
            var size = box.Size;
            var longestSide = MathF.Max(size.X, size.Y);
            var longestRotatedSide = Math.Max(longestSide, (size.X + size.Y) / MathF.Sqrt(2));
            _spriteSize = new Vector2(longestRotatedSide, longestRotatedSide);
        }

        internal override void DrawInternal(IRenderHandle renderHandle)
        {
            if (Entity is not {} uid || _sprite == null)
                return;

            if (_sprite.Deleted)
            {
                SetEntity(null);
                return;
            }

            // Ensure the sprite is animated despite possible not being visible in any viewport.
            _spriteSystem ??= _entMan.System<SpriteSystem>();
            _spriteSystem.ForceUpdate(uid);

            var stretch = Stretch switch
            {
                StretchMode.Fit => Vector2.ComponentMin(Size / _spriteSize, Vector2.One),
                StretchMode.Fill => Size / _spriteSize,
                _ => Vector2.One,
            };

            var offset = SpriteOffset
                ? Vector2.Zero
                : - (-_eyeRotation).RotateVec(_sprite.Offset) * (1, -1) * EyeManager.PixelsPerMeter;

            var position = PixelSize / 2 + offset * stretch * UIScale;
            var scale = Scale * UIScale * stretch;
            renderHandle.DrawEntity(uid, position, scale, _worldRotation, _eyeRotation, OverrideDirection, _sprite);
        }
    }
}
