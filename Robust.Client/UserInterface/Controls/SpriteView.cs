using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
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
        protected SpriteSystem? SpriteSystem;
        private SharedTransformSystem? _transform;
        protected readonly IEntityManager EntMan;

        [ViewVariables]
        public SpriteComponent? Sprite => Entity?.Comp1;

        [ViewVariables]
        public Entity<SpriteComponent, TransformComponent>? Entity { get; private set; }

        [ViewVariables]
        public NetEntity? NetEnt { get; private set; }

        /// <summary>
        /// This field configures automatic scaling of the sprite. This automatic scaling is done before
        /// applying the explicitly set scale <see cref="SpriteView.Scale"/>.
        /// </summary>
        public StretchMode Stretch  { get; set; } = StretchMode.Fit;

        public enum StretchMode
        {
            /// <summary>
            /// Don't automatically scale the sprite. The sprite can still be scaled via <see cref="SpriteView.Scale"/>
            /// </summary>
            None,

            /// <summary>
            /// Scales the sprite down so that it fits within the control. Does not scale the sprite up. Keeps the same
            /// aspect ratio. This automatic scaling is done before applying <see cref="SpriteView.Scale"/>.
            /// </summary>
            Fit,

            /// <summary>
            /// Scale the sprite up or down so that it fills the whole control. Keeps the same aspect ratio. This
            /// automatic scaling is done before applying <see cref="SpriteView.Scale"/>.
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
            IoCManager.Resolve(ref EntMan);
            RectClipContent = true;
        }

        public SpriteView(IEntityManager entMan)
        {
            EntMan = entMan;
            RectClipContent = true;
        }

        public SpriteView(EntityUid? uid, IEntityManager entMan)
        {
            EntMan = entMan;
            RectClipContent = true;
            SetEntity(uid);
        }

        public SpriteView(NetEntity uid, IEntityManager entMan)
        {
            EntMan = entMan;
            RectClipContent = true;
            SetEntity(uid);
        }

        public void SetEntity(NetEntity netEnt)
        {
            if (netEnt == NetEnt)
                return;

            // The Entity is getting set later in the ResolveEntity method
            // because the client may not have received it yet.
            Entity = null;
            NetEnt = netEnt;
        }

        public void SetEntity(EntityUid? uid)
        {
            if (Entity?.Owner == uid)
                return;

            if (!EntMan.TryGetComponent(uid, out SpriteComponent? sprite)
                || !EntMan.TryGetComponent(uid, out TransformComponent? xform))
            {
                Entity = null;
                NetEnt = null;
                return;
            }

            Entity = new(uid.Value, sprite, xform);
            NetEnt = EntMan.GetNetEntity(uid);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            // TODO Make this get called when sprite bounds/properties update?
            UpdateSize();
            return _spriteSize;
        }

        private void UpdateSize()
        {
            if (!ResolveEntity(out _, out var sprite, out _))
                return;

            var spriteBox = sprite.CalculateRotatedBoundingBox(default,  _worldRotation ?? Angle.Zero, _eyeRotation)
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
            tr = Vector2.Max(tr, Vector2.Zero);
            bl = Vector2.Min(bl, Vector2.Zero);
            tr = Vector2.Max(tr, -bl);
            bl = Vector2.Min(bl, -tr);
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

        protected internal override void Draw(IRenderHandle renderHandle)
        {
            if (!ResolveEntity(out var uid, out var sprite, out var xform))
                return;

            SpriteSystem ??= EntMan.System<SpriteSystem>();
            _transform ??= EntMan.System<TransformSystem>();

            // Ensure the sprite is animated despite possible not being visible in any viewport.
            SpriteSystem.ForceUpdate(uid);

            var stretchVec = Stretch switch
            {
                StretchMode.Fit => Vector2.Min(Size / _spriteSize, Vector2.One),
                StretchMode.Fill => Size / _spriteSize,
                _ => Vector2.One,
            };
            var stretch = MathF.Min(stretchVec.X, stretchVec.Y);

            var offset = SpriteOffset
                ? Vector2.Zero
                : - (-_eyeRotation).RotateVec(sprite.Offset * _scale) * new Vector2(1, -1) * EyeManager.PixelsPerMeter;

            var position = PixelSize / 2 + offset * stretch * UIScale;
            var scale = Scale * UIScale * stretch;

            // control modulation is applied automatically to the screen handle, but here we need to use the world handle
            var world = renderHandle.DrawingHandleWorld;
            var oldModulate = world.Modulate;
            world.Modulate *= Modulate * ActualModulateSelf;

            renderHandle.DrawEntity(uid, position, scale, _worldRotation, _eyeRotation, OverrideDirection, sprite, xform, _transform);
            world.Modulate = oldModulate;
        }

        private bool ResolveEntity(
            out EntityUid uid,
            [NotNullWhen(true)] out SpriteComponent? sprite,
            [NotNullWhen(true)] out TransformComponent? xform)
        {
            if (NetEnt != null && Entity == null && EntMan.TryGetEntity(NetEnt, out var ent))
                SetEntity(ent);

            if (Entity != null)
            {
                (uid, sprite, xform) = Entity.Value;
                return !EntMan.Deleted(uid);
            }

            sprite = null;
            xform = null;
            uid = default;
            return false;
        }
    }
}
