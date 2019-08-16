using System;
using JetBrains.Annotations;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public abstract class ScrollBar : Range
    {
        public const string StylePropertyGrabber = "grabber";
        public const string StylePseudoClassHover = "hover";
        public const string StylePseudoClassGrabbed = "grabbed";

        private readonly OrientationMode _orientation;
        private bool _isHovered;
        private (Vector2 pos, float value)? _grabData;

        protected ScrollBar(OrientationMode orientation)
        {
            _orientation = orientation;

            MouseFilter = MouseFilterMode.Pass;
        }

        public bool IsAtEnd
        {
            get
            {
                var offset = Value + Page;
                return offset > MaxValue || FloatMath.CloseTo(offset, MaxValue);
            }
        }

        public void MoveToEnd()
        {
            // Will be clamped as necessary.
            Value = MaxValue;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            var styleBox = _getGrabberStyleBox();

            styleBox?.Draw(handle, _getGrabberBox());
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

            if (args.Function != EngineKeyFunctions.Use)
            {
                return;
            }

            var box = _getGrabberBox();
            if (!box.Contains(args.RelativePosition))
            {
                return;
            }

            _grabData = (args.RelativePosition, Value);
            _updatePseudoClass();
        }

        protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (args.Function != EngineKeyFunctions.Use)
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
                _isHovered = box.Contains(args.RelativePosition);
                _updatePseudoClass();
                return;
            }

            var (grabPos, grabValue) = _grabData.Value;
            var (grabRelX, grabRelY) = args.RelativePosition - grabPos;
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

            var grabberEnd = (Value + Page - MinValue) / (MaxValue - MinValue) * _getOrientationSize();
            grabberEnd = (float) Math.Round(grabberEnd);

            if (_orientation == OrientationMode.Horizontal)
            {
                return new UIBox2(grabberOffset, 0, grabberEnd, PixelHeight);
            }

            return new UIBox2(0, grabberOffset, PixelWidth, grabberEnd);
        }

        [System.Diagnostics.Contracts.Pure]
        [CanBeNull]
        private StyleBox _getGrabberStyleBox()
        {
            if (TryGetStyleProperty(StylePropertyGrabber, out StyleBox styleBox))
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
                return PixelWidth;
            }

            return PixelHeight;
        }

        private void _updatePseudoClass()
        {
            if (_grabData != null)
            {
                StylePseudoClass = StylePseudoClassGrabbed;
            }
            else if (_isHovered)
            {
                StylePseudoClass = StylePseudoClassHover;
            }
            else
            {
                StylePseudoClass = null;
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return _getGrabberStyleBox()?.MinimumSize ?? Vector2.Zero;
        }

        protected enum OrientationMode
        {
            Horizontal,
            Vertical
        }
    }
}
