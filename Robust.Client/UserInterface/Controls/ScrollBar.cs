using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public abstract class ScrollBar : Range
    {
        public const string StylePropertyGrabber = "grabber";
        public const string StylePseudoClassHover = "hover";
        public const string StylePseudoClassGrabbed = "grabbed";

        private readonly OrientationMode _orientation;
        private bool _isHovered;
        private (Vector2 pos, float value)? _grabData;
        private float _valueTarget;

        public float ValueTarget
        {
            get => _valueTarget;
            set => _valueTarget = ClampValue(value);
        }

        private bool _updating;

        public override float Value
        {
            get => base.Value;
            set
            {
                if (!_updating)
                {
                    ValueTarget = value;
                }
                base.Value = value;
            }
        }

        protected ScrollBar(OrientationMode orientation)
        {
            MouseFilter = MouseFilterMode.Pass;
            ReservesSpace = true;

            _orientation = orientation;
        }

        public bool IsAtEnd
        {
            get
            {
                var offset = ValueTarget + Page;
                return offset > MaxValue || MathHelper.CloseToPercent(offset, MaxValue);
            }
        }

        public void MoveToEnd()
        {
            // Will be clamped as necessary.
            ValueTarget = MaxValue;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!VisibleInTree || MathHelper.CloseToPercent(Value, ValueTarget))
            {
                Value = ValueTarget;
            }
            else
            {
                _updating = true;
                Value = UIAnimations.LerpAnimate(Value, ValueTarget, args.DeltaSeconds, 15);
                _updating = false;
            }
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            var styleBox = _getGrabberStyleBox();
            styleBox?.Draw(handle, _getGrabberBox(), UIScale);
        }

        protected internal override void MouseExited()
        {
            base.MouseExited();

            _isHovered = false;
            _updatePseudoClass();
        }

        protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (args.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            var box = _getGrabberBox();
            if (!box.Contains(args.RelativePixelPosition))
            {
                return;
            }

            _grabData = (args.RelativePixelPosition, Value);
            _updatePseudoClass();
            args.Handle();
        }

        protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (args.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            _grabData = null;
            _updatePseudoClass();
        }

        protected internal override void MouseMove(GUIMouseMoveEventArgs args)
        {
            if (_grabData == null)
            {
                var box = _getGrabberBox();
                _isHovered = box.Contains(args.RelativePixelPosition);
                _updatePseudoClass();
                return;
            }

            var (grabPos, grabValue) = _grabData.Value;
            var (grabRelX, grabRelY) = args.RelativePixelPosition - grabPos;
            float moved;

            if (_orientation == OrientationMode.Horizontal)
            {
                moved = grabRelX;
            }
            else
            {
                moved = grabRelY;
            }

            var movedValue = moved / _getOrientationSize();
            movedValue *= MaxValue - MinValue;
            movedValue += MinValue + grabValue;
            Value = movedValue;
        }

        [System.Diagnostics.Contracts.Pure]
        private UIBox2 _getGrabberBox()
        {
            var grabberOffset = GetAsRatio() * _getOrientationSize();
            grabberOffset = (float) Math.Round(grabberOffset);

            var grabberEnd = (Value + Page - MinValue) / (MaxValue - MinValue) * _getOrientationSize() + _getGrabberBoxMinSize();
            grabberEnd = (float) Math.Round(grabberEnd);

            if (_orientation == OrientationMode.Horizontal)
            {
                return new UIBox2(grabberOffset, 0, grabberEnd, PixelHeight);
            }

            return new UIBox2(0, grabberOffset, PixelWidth, grabberEnd);
        }

        private float _getGrabberBoxMinSize()
        {
            var styleBox = _getGrabberStyleBox();
            if (styleBox == null)
            {
                return 0;
            }

            return _orientation == OrientationMode.Horizontal ? styleBox.MinimumSize.X : styleBox.MinimumSize.Y;
        }

        [System.Diagnostics.Contracts.Pure]
        private StyleBox? _getGrabberStyleBox()
        {
            if (TryGetStyleProperty<StyleBox>(StylePropertyGrabber, out var styleBox))
            {
                return styleBox;
            }

            return null;
        }

        [System.Diagnostics.Contracts.Pure]
        private float _getOrientationSize()
        {
            if (_orientation == OrientationMode.Horizontal)
            {
                return PixelWidth - _getGrabberBoxMinSize();
            }

            return PixelHeight - _getGrabberBoxMinSize();
        }

        private void _updatePseudoClass()
        {
            if (_grabData != null)
            {
                SetOnlyStylePseudoClass(StylePseudoClassGrabbed);
            }
            else if (_isHovered)
            {
                SetOnlyStylePseudoClass(StylePseudoClassHover);
            }
            else
            {
                SetOnlyStylePseudoClass(null);
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return _getGrabberStyleBox()?.MinimumSize ?? Vector2.Zero;
        }

        protected enum OrientationMode : byte
        {
            Horizontal,
            Vertical
        }
    }
}
