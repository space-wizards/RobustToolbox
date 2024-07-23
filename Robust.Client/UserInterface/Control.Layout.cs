using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface
{
    // Code and design heavily inspired by WPF/Avalonia.

    public partial class Control
    {
        private const float DefaultStretchRatio = 1;
        private const float DefaultSetSize = float.NaN;
        private const float DefaultMaxSize = float.PositiveInfinity;
        private const HAlignment DefaultHAlignment = HAlignment.Stretch;
        private const VAlignment DefaultVAlignment = VAlignment.Stretch;

        private Vector2 _size;

        [ViewVariables] internal Vector2? PreviousMeasure;
        [ViewVariables] internal UIBox2? PreviousArrange;

        private float _sizeFlagsStretchRatio = DefaultStretchRatio;

        private float _minWidth;
        private float _minHeight;
        private float _setWidth = DefaultSetSize;
        private float _setHeight = DefaultSetSize;
        private float _maxWidth = DefaultMaxSize;
        private float _maxHeight = DefaultMaxSize;

        private bool _horizontalExpand;
        private bool _verticalExpand;
        private HAlignment _horizontalAlignment = DefaultHAlignment;
        private VAlignment _verticalAlignment = DefaultVAlignment;
        private Thickness _margin;
        private bool _measuring;
        private bool _arranging;

        /// <summary>
        /// The desired minimum size this control needs for layout to avoid cutting off content or such.
        /// </summary>
        /// <remarks>
        /// This is calculated by calling <see cref="Measure"/>.
        /// </remarks>
        [ViewVariables] public Vector2 DesiredSize { get; private set; }
        [ViewVariables] public Vector2i DesiredPixelSize => (Vector2i) (DesiredSize * UIScale);

        [ViewVariables] public bool IsMeasureValid { get; private set; }
        [ViewVariables] public bool IsArrangeValid { get; private set; }

        /// <summary>
        /// Controls the amount of empty space in virtual pixels around the control.
        /// </summary>
        /// <remarks>Values can be provided as "All" or "Horizontal, Vertical" or "Left, Top, Right, Bottom"</remarks>
        [ViewVariables]
        public Thickness Margin
        {
            get => _margin;
            set
            {
                _margin = value;
                SetLayoutStyleProp(LayoutStyleProperties.Margin);
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     Called when the <see cref="UIScale"/> for this control changes.
        /// </summary>
        protected internal virtual void UIScaleChanged()
        {
            InvalidateMeasure();
        }

        /// <summary>
        ///     The amount of "real" pixels a virtual pixel takes up.
        ///     The higher the number, the bigger the interface.
        ///     I.e. UIScale units are real pixels (rp) / virtual pixels (vp),
        ///     real pixels varies depending on interface, virtual pixels doesn't.
        ///     And vp * UIScale = rp, and rp / UIScale = vp
        /// </summary>
        [ViewVariables]
        public virtual float UIScale => Root?.UIScale ?? 1;

        /// <summary>
        ///     The size of this control, in virtual pixels.
        /// </summary>
        /// <seealso cref="PixelSize"/>
        /// <seealso cref="Width"/>
        /// <seealso cref="Height"/>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Size
        {
            get => _size;
            internal set
            {
                if (_size == value)
                {
                    return;
                }

                _size = value;
                Resized();
            }
        }

        /// <summary>
        ///     The size of this control, in physical pixels.
        /// </summary>
        [ViewVariables]
        public Vector2i PixelSize => (Vector2i) (_size * UIScale);

        /// <summary>
        ///     A <see cref="UIBox2"/> with the top left at 0,0 and the size equal to <see cref="Size"/>.
        /// </summary>
        /// <seealso cref="PixelSizeBox"/>
        public UIBox2 SizeBox => new(Vector2.Zero, Size);

        /// <summary>
        ///     A <see cref="UIBox2i"/> with the top left at 0,0 and the size equal to <see cref="PixelSize"/>.
        /// </summary>
        /// <seealso cref="SizeBox"/>
        public UIBox2i PixelSizeBox => new(Vector2i.Zero, PixelSize);

        /// <summary>
        ///     The width of the control, in virtual pixels.
        /// </summary>
        /// <seealso cref="PixelWidth"/>
        public float Width => Size.X;

        /// <summary>
        ///     The height of the control, in virtual pixels.
        /// </summary>
        /// <seealso cref="PixelHeight"/>
        public float Height => Size.Y;

        /// <summary>
        ///     The width of the control, in physical pixels.
        /// </summary>
        /// <seealso cref="Width"/>
        public int PixelWidth => PixelSize.X;

        /// <summary>
        ///     The height of the control, in physical pixels.
        /// </summary>
        /// <seealso cref="Height"/>
        public int PixelHeight => PixelSize.Y;

        /// <summary>
        ///     The position of the top left corner of the control, in virtual pixels.
        ///     This is relative to the position of the parent.
        /// </summary>
        /// <seealso cref="PixelPosition"/>
        /// <seealso cref="GlobalPosition"/>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Position { get; internal set; }

        /// <summary>
        ///     The position of the top left corner of the control, in physical pixels.
        /// </summary>
        /// <seealso cref="Position"/>
        [ViewVariables]
        public Vector2i PixelPosition => (Vector2i) (Position * UIScale);

        /// <summary>
        ///     The position of the top left corner of the control, in virtual pixels.
        ///     This is not relative to the parent.
        /// </summary>
        /// <seealso cref="GlobalPosition"/>
        /// <seealso cref="Position"/>
        [ViewVariables]
        public Vector2 GlobalPosition
        {
            get
            {
                var offset = Position;
                var parent = Parent;
                while (parent != null)
                {
                    offset += parent.Position;
                    parent = parent.Parent;
                }

                return offset;
            }
        }

        /// <summary>
        ///     The position of the top left corner of the control, in physical pixels.
        ///     This is not relative to the parent.
        /// </summary>
        /// <seealso cref="GlobalPosition"/>
        [ViewVariables]
        public Vector2i GlobalPixelPosition
        {
            get
            {
                var offset = PixelPosition;
                var parent = Parent;
                while (parent != null)
                {
                    offset += parent.PixelPosition;
                    parent = parent.Parent;
                }

                return offset;
            }
        }

        [ViewVariables] public virtual IClydeWindow? Window => Root?.Window;

        [ViewVariables]
        public virtual ScreenCoordinates ScreenCoordinates
        {
            get
            {
                // TODO: optimize for single tree walk.
                var window = Window;

                return window != null ? new(GlobalPixelPosition, window.Id) : default;
            }
        }

        /// <summary>
        ///     Represents the "rectangle" of the control relative to the parent, in virtual pixels.
        /// </summary>
        /// <seealso cref="PixelRect"/>
        public UIBox2 Rect => UIBox2.FromDimensions(Position, _size);

        /// <summary>
        ///     Represents the "rectangle" of the control relative to the parent, in physical pixels.
        /// </summary>
        /// <seealso cref="Rect"/>
        public UIBox2i PixelRect => UIBox2i.FromDimensions(PixelPosition, PixelSize);

        public UIBox2 GlobalRect => UIBox2.FromDimensions(GlobalPosition, _size);

        public UIBox2i GlobalPixelRect => UIBox2i.FromDimensions(GlobalPixelPosition, PixelSize);

        /// <summary>
        /// Horizontal alignment mode.
        /// This determines how the control should be laid out horizontally
        /// if it gets more available space than its <see cref="DesiredSize"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public HAlignment HorizontalAlignment
        {
            get => _horizontalAlignment;
            set
            {
                _horizontalAlignment = value;
                SetLayoutStyleProp(LayoutStyleProperties.HorizontalAlignment);
                InvalidateArrange();
            }
        }

        /// <summary>
        /// Vertical alignment mode.
        /// This determines how the control should be laid out vertically
        /// if it gets more available space than its <see cref="DesiredSize"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public VAlignment VerticalAlignment
        {
            get => _verticalAlignment;
            set
            {
                _verticalAlignment = value;
                SetLayoutStyleProp(LayoutStyleProperties.VerticalAlignment);
                InvalidateArrange();
            }
        }

        /// <summary>
        /// Whether to horizontally expand and push other controls in layout controls that support this.
        /// This does nothing unless the parent is a control like <see cref="BoxContainer"/> which supports this behavior.
        /// </summary>
        /// <remarks>
        /// If I was redesigning the UI system from scratch today, this would be an attached property instead.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool HorizontalExpand
        {
            get => _horizontalExpand;
            set
            {
                _horizontalExpand = value;
                SetLayoutStyleProp(LayoutStyleProperties.HorizontalExpand);
                Parent?.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Whether to vertically expand and push other controls in layout controls that support this.
        /// This does nothing unless the parent is a control like <see cref="BoxContainer"/> which supports this behavior.
        /// </summary>
        /// <remarks>
        /// If I was redesigning the UI system from scratch today, this would be an attached property instead.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool VerticalExpand
        {
            get => _verticalExpand;
            set
            {
                _verticalExpand = value;
                SetLayoutStyleProp(LayoutStyleProperties.VerticalExpand);
                Parent?.InvalidateArrange();
            }
        }

        /// <summary>
        ///     Stretch ratio used to give shared of the available space in case multiple siblings are set to expand
        ///     in a container
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if the value is less than or equal to 0.
        /// </exception>
        [ViewVariables]
        public float SizeFlagsStretchRatio
        {
            get => _sizeFlagsStretchRatio;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be greater than zero.");
                }

                _sizeFlagsStretchRatio = value;

                SetLayoutStyleProp(LayoutStyleProperties.StretchRatio);
                Parent?.InvalidateArrange();
            }
        }

        /// <summary>
        /// A settable minimum size for this control.
        /// This is factored into <see cref="MeasureCore"/> so that this control itself always has at least this size.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is not to be confused with <see cref="DesiredSize"/>,
        /// which contains the actual calculated minimum size from the layout system.
        /// This property is just an input parameter.
        /// </para>
        /// <para>
        /// If <see cref="MaxSize"/>, <see cref="MinSize"/> and/or <see cref="SetSize"/> are in conflict,
        /// <see cref="MinSize"/> is the most important, then <see cref="MaxSize"/>, then <see cref="SetSize"/>.
        /// </para>
        /// </remarks>
        /// <seealso cref="MinWidth"/>
        /// <seealso cref="MinHeight"/>
        public Vector2 MinSize
        {
            get => new(_minWidth, _minHeight);
            set => (MinWidth, MinHeight) = Vector2.Max(Vector2.Zero, value);
        }

        /// <summary>
        /// A settable exact size for this control.
        /// This is factored into <see cref="MeasureCore"/> so that this control itself always has exactly this size.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is not to be confused with <see cref="Size"/>,
        /// which contains the actual calculated size from the layout system.
        /// This property is just an input parameter.
        /// </para>
        /// <para>
        /// If <see cref="MaxSize"/>, <see cref="MinSize"/> and/or <see cref="SetSize"/> are in conflict,
        /// <see cref="MinSize"/> is the most important, then <see cref="MaxSize"/>, then <see cref="SetSize"/>.
        /// </para>
        /// </remarks>
        /// <seealso cref="SetWidth"/>
        /// <seealso cref="SetHeight"/>
        public Vector2 SetSize
        {
            get => new(_setWidth, _setHeight);
            set => (SetWidth, SetHeight) = value;
        }

        /// <summary>
        /// A settable maximum size for this control.
        /// This is factored into <see cref="MeasureCore"/> so that this control itself always has at most this size.
        /// </summary>
        /// <remarks>
        /// If <see cref="MaxSize"/>, <see cref="MinSize"/> and/or <see cref="SetSize"/> are in conflict,
        /// <see cref="MinSize"/> is the most important, then <see cref="MaxSize"/>, then <see cref="SetSize"/>.
        /// </remarks>
        /// <seealso cref="MaxWidth"/>
        /// <seealso cref="MaxHeight"/>
        public Vector2 MaxSize
        {
            get => new(_maxWidth, _maxHeight);
            set => (MaxWidth, MaxHeight) = value;
        }

        /// <summary>
        /// Width component of <see cref="MinSize"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MinWidth
        {
            get => _minWidth;
            set
            {
                _minWidth = value;
                SetLayoutStyleProp(LayoutStyleProperties.MinWidth);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Height component of <see cref="MinSize"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MinHeight
        {
            get => _minHeight;
            set
            {
                _minHeight = value;
                SetLayoutStyleProp(LayoutStyleProperties.MinHeight);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Width component of <see cref="SetSize"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SetWidth
        {
            get => _setWidth;
            set
            {
                _setWidth = value;
                SetLayoutStyleProp(LayoutStyleProperties.SetWidth);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Height component of <see cref="SetSize"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SetHeight
        {
            get => _setHeight;
            set
            {
                _setHeight = value;
                SetLayoutStyleProp(LayoutStyleProperties.SetHeight);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Width component of <see cref="MaxSize"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxWidth
        {
            get => _maxWidth;
            set
            {
                _maxWidth = value;
                SetLayoutStyleProp(LayoutStyleProperties.MaxWidth);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Height component of <see cref="MaxSize"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxHeight
        {
            get => _maxHeight;
            set
            {
                _maxHeight = value;
                SetLayoutStyleProp(LayoutStyleProperties.MaxHeight);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets the screen coordinates position relative to the control.
        /// </summary>
        public Vector2 GetLocalPosition(ScreenCoordinates coordinates)
        {
            return coordinates.Position - GlobalPixelPosition;
        }

        /// <summary>
        /// Notify the layout system that this control's <see cref="Measure"/> result may have changed
        /// and must be recalculated.
        /// </summary>
        public void InvalidateMeasure()
        {
            if (!IsMeasureValid || _measuring)
                return;

            IsMeasureValid = false;
            UserInterfaceManagerInternal.QueueMeasureUpdate(this);
            InvalidateArrange();
        }

        /// <summary>
        /// Notify the layout system that this control's <see cref="Arrange"/> result may have changed
        /// and must be recalculated.
        /// </summary>
        public void InvalidateArrange()
        {
            if (!IsArrangeValid || _arranging)
            {
                // Already queued for a layout update, don't bother.
                return;
            }

            IsArrangeValid = false;
            UserInterfaceManagerInternal.QueueArrangeUpdate(this);
        }

        /// <summary>
        /// Measure the desired size of this control, if given a specific available space.
        /// The result of this measure is stored in <see cref="DesiredSize"/>.
        /// </summary>
        /// <remarks>
        /// Available size is given to this method so that controls can handle special cases such as text layout,
        /// where word wrapping can cause the vertical size to change based on available horizontal size.
        /// </remarks>
        /// <param name="availableSize">The space available to this control, that it should measure for.</param>
        public void Measure(Vector2 availableSize)
        {
            if (!IsMeasureValid || PreviousMeasure != availableSize)
            {
                IsMeasureValid = true;
                _measuring = true;
                Vector2 desired;
                try
                {
                    desired = MeasureCore(availableSize);
                }
                finally
                {
                    _measuring = false;
                }

                if (desired.X < 0 || desired.Y < 0 || !float.IsFinite(desired.X) || !float.IsFinite(desired.Y))
                    throw new InvalidOperationException("Invalid size returned from Measure()");

                var prev = DesiredSize;
                DesiredSize = desired;
                PreviousMeasure = availableSize;

                if (prev != desired && Parent != null && !Parent._measuring)
                    Parent?.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Core logic implementation of <see cref="Measure"/>,
        /// implementing stuff such as margins and <see cref="MinSize"/>.
        /// In almost all cases, you want to override <see cref="MeasureOverride"/> instead, which is called by this.
        /// </summary>
        /// <returns>The actual measured desired size of the control.</returns>
        protected virtual Vector2 MeasureCore(Vector2 availableSize)
        {
            if (!(Visible || ReservesSpace))
                return default;

            if (_stylingDirty)
                ForceRunStyleUpdate();

            var withoutMargin = _margin.Deflate(availableSize);

            var constrained = ApplySizeConstraints(this, withoutMargin);

            var measured = MeasureOverride(constrained);

            if (!float.IsNaN(SetWidth))
            {
                measured.X = SetWidth;
            }

            measured.X = Math.Clamp(measured.X, MinWidth, MaxWidth);

            if (!float.IsNaN(SetHeight))
            {
                measured.Y = SetHeight;
            }

            measured.Y = Math.Clamp(measured.Y, MinHeight, MaxHeight);

            measured = _margin.Inflate(measured);
            measured = Vector2.Min(measured, availableSize);
            measured = Vector2.Max(measured, Vector2.Zero);
            return measured;
        }

        /// <summary>
        /// Calculates the actual desired size for the contents of this control, based on available size.
        /// </summary>
        protected virtual Vector2 MeasureOverride(Vector2 availableSize)
        {
            var min = Vector2.Zero;

            foreach (var child in Children)
            {
                child.Measure(availableSize);
                min = Vector2.Max(min, child.DesiredSize);
            }

            return min;
        }

        /// <summary>
        /// Lay out this control in the given space of its parent, by pixel coordinates.
        /// </summary>
        public void ArrangePixel(UIBox2i finalRect)
        {
            var topLeft = finalRect.TopLeft / UIScale;
            var bottomRight = finalRect.BottomRight / UIScale;

            Arrange(new UIBox2(topLeft, bottomRight));
        }

        /// <summary>
        /// Lay out this control in the given space of its parent.
        /// This sets <see cref="Position"/> and <see cref="Size"/> and also arranges any child controls.
        /// </summary>
        public void Arrange(UIBox2 finalRect)
        {
            _arranging = true;
            try
            {
                if (!IsMeasureValid)
                    Measure(PreviousMeasure ?? finalRect.Size);

                if (!IsArrangeValid || PreviousArrange != finalRect)
                {
                    IsArrangeValid = true;
                    ArrangeCore(finalRect);
                    PreviousArrange = finalRect;
                }
            }
            finally
            {
                _arranging = false;
            }
        }

        /// <summary>
        /// Core logic implementation of <see cref="Arrange"/>,
        /// implementing stuff such as margins and <see cref="MinSize"/>.
        /// In almost all cases, you want to override <see cref="ArrangeOverride"/> instead, which is called by this.
        /// </summary>
        protected virtual void ArrangeCore(UIBox2 finalRect)
        {
            if (!(Visible || ReservesSpace))
                return;

            var withoutMargins = _margin.Deflate(finalRect);
            var availWithoutMargins = withoutMargins.Size;
            var size = availWithoutMargins;
            var origin = withoutMargins.TopLeft;

            if (_horizontalAlignment != HAlignment.Stretch)
                size.X = Math.Min(size.X, DesiredSize.X - _margin.SumHorizontal);

            if (_verticalAlignment != VAlignment.Stretch)
                size.Y = Math.Min(size.Y, DesiredSize.Y - _margin.SumVertical);


            size = ApplySizeConstraints(this, size);

            var arranged = ArrangeOverride(size);

            size = Vector2.Min(arranged, size);

            switch (HorizontalAlignment)
            {
                case HAlignment.Stretch:
                case HAlignment.Center:
                    origin.X += (availWithoutMargins.X - size.X) / 2;
                    break;
                case HAlignment.Right:
                    origin.X += availWithoutMargins.X - size.X;
                    break;
            }

            switch (VerticalAlignment)
            {
                case VAlignment.Stretch:
                case VAlignment.Center:
                    origin.Y += (availWithoutMargins.Y - size.Y) / 2;
                    break;
                case VAlignment.Bottom:
                    origin.Y += availWithoutMargins.Y - size.Y;
                    break;
            }

            Position = origin;
            Size = size;
        }

        /// <summary>
        /// Lay out this control and its children for the specified final size.
        /// </summary>
        /// <param name="finalSize">
        /// The final size for this control,
        /// after calculation of things like margins and alignment.
        /// </param>
        /// <returns>The actual space used by this control.</returns>
        protected virtual Vector2 ArrangeOverride(Vector2 finalSize)
        {
            foreach (var child in Children)
            {
                child.Arrange(UIBox2.FromDimensions(Vector2.Zero, finalSize));
            }

            return finalSize;
        }

        private static Vector2 ApplySizeConstraints(Control control, Vector2 avail)
        {
            var minW = control._minWidth;
            var setW = control._setWidth;
            var maxW = control._maxWidth;

            var maxConstraint = float.IsNaN(setW) ? float.PositiveInfinity : setW;
            maxW = MathHelper.Clamp(maxConstraint, minW, maxW);

            var minConstraint = float.IsNaN(setW) ? 0 : setW;
            minW = MathHelper.Clamp(maxW, minConstraint, minW);

            var minH = control._minHeight;
            var setH = control._setHeight;
            var maxH = control._maxHeight;

            maxConstraint = float.IsNaN(setH) ? float.PositiveInfinity : setH;
            maxH = MathHelper.Clamp(maxConstraint, minH, maxH);

            minConstraint = float.IsNaN(setH) ? 0 : setH;
            minH = MathHelper.Clamp(minConstraint, minH, maxH);

            return new Vector2(
                Math.Clamp(avail.X, minW, maxW),
                Math.Clamp(avail.Y, minH, maxH));
        }

        /// <summary>
        /// Specifies horizontal alignment modes.
        /// </summary>
        /// <seealso cref="Control.HorizontalAlignment"/>
        public enum HAlignment
        {
            /// <summary>
            /// The control should take up all available horizontal space.
            /// </summary>
            Stretch,

            /// <summary>
            /// The control should take up minimal (<see cref="Control.DesiredSize"/>) space and align to the left of its given space.
            /// </summary>
            Left,

            /// <summary>
            /// The control should take up minimal (<see cref="Control.DesiredSize"/>) space and align in the center of its given space.
            /// </summary>
            Center,

            /// <summary>
            /// The control should take up minimal (<see cref="Control.DesiredSize"/>) space and align to the right of its given space.
            /// </summary>
            Right
        }

        /// <summary>
        /// Specifies vertical alignment modes.
        /// </summary>
        /// <seealso cref="Control.VerticalAlignment"/>
        public enum VAlignment
        {
            /// <summary>
            /// The control should take up all available vertical space.
            /// </summary>
            Stretch,

            /// <summary>
            /// The control should take up minimal (<see cref="Control.DesiredSize"/>) space and align to the top of its given space.
            /// </summary>
            Top,

            /// <summary>
            /// The control should take up minimal (<see cref="Control.DesiredSize"/>) space and align in the center of its given space.
            /// </summary>
            Center,

            /// <summary>
            /// The control should take up minimal (<see cref="Control.DesiredSize"/>) space and align to the bottom of its given space.
            /// </summary>
            Bottom
        }
    }
}
