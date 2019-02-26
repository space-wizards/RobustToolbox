using System;
using JetBrains.Annotations;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Input;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.ScrollBar))]
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
        }

        private protected ScrollBar(Godot.ScrollBar control) : base(control)
        {
        }

        public bool IsAtEnd
        {
            get
            {
                var offset = Value + Page;
                return FloatMath.CloseTo(offset, MaxValue);
            }
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            if (GameController.OnGodot)
            {
                return;
            }

            var styleBox = _getGrabberStyleBox();

            styleBox?.Draw(handle, _getGrabberBox());
        }

        protected override void SetDefaults()
        {
            base.SetDefaults();

            MouseFilter = MouseFilterMode.Pass;
        }

        protected internal override void MouseExited()
        {
            base.MouseExited();

            if (GameController.OnGodot)
            {
                return;
            }

            _isHovered = false;
            _updatePseudoClass();
        }

        protected internal override void MouseMove(GUIMouseMoveEventArgs args)
        {
            if (GameController.OnGodot)
            {
                return;
            }

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

        protected internal override void MouseDown(GUIMouseButtonEventArgs args)
        {
            if (GameController.OnGodot || args.Button != Mouse.Button.Left)
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

        protected internal override void MouseUp(GUIMouseButtonEventArgs args)
        {
            if (GameController.OnGodot || args.Button != Mouse.Button.Left)
            {
                return;
            }

            _grabData = null;
            _updatePseudoClass();
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
                return new UIBox2(grabberOffset, 0, grabberEnd, Height);
            }
            else
            {
                return new UIBox2(0, grabberOffset, Width, grabberEnd);
            }
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
                return Width;
            }
            else
            {
                return Height;
            }
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
            Vertical,
        }
    }
}
