using System;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface
{
    // Code and design heavily inspired by WPF/Avalonia.

    public partial class Control
    {
        private Vector2 _size;

        [ViewVariables] internal Vector2? PreviousMeasure;
        [ViewVariables] internal UIBox2? PreviousArrange;

        private float _sizeFlagsStretchRatio = 1;

        private float _minWidth = 0;
        private float _minHeight = 0;
        private float _setWidth = float.NaN;
        private float _setHeight = float.NaN;
        private float _maxWidth = float.PositiveInfinity;
        private float _maxHeight = float.PositiveInfinity;

        private bool _horizontalExpand;
        private bool _verticalExpand;
        private HAlignment _horizontalAlignment;
        private VAlignment _verticalAlignment;
        private Thickness _margin;
        private bool _isLayoutUpdateOverrideUsed;
        private bool _measuring;

        [ViewVariables] public Vector2 DesiredSize { get; private set; }
        [ViewVariables] public Vector2i DesiredPixelSize => (Vector2i) (DesiredSize * UIScale);
        [ViewVariables] public bool IsMeasureValid { get; private set; }
        [ViewVariables] public bool IsArrangeValid { get; private set; }

        [ViewVariables]
        public Thickness Margin
        {
            get => _margin;
            set => _margin = value;
        }

        /// <summary>
        ///     Called when the <see cref="UIScale"/> for this control changes.
        /// </summary>
        protected internal virtual void UIScaleChanged()
        {
            MinimumSizeChanged();
        }

        /// <summary>
        ///     The amount of "real" pixels a virtual pixel takes up.
        ///     The higher the number, the bigger the interface.
        ///     I.e. UIScale units are real pixels (rp) / virtual pixels (vp),
        ///     real pixels varies depending on interface, virtual pixels doesn't.
        ///     And vp * UIScale = rp, and rp / UIScale = vp
        /// </summary>
        [ViewVariables]
        protected float UIScale => UserInterfaceManager.UIScale;

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
        public Vector2i PixelSize => (Vector2i) (_size * UserInterfaceManager.UIScale);

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
        public Vector2i PixelPosition => (Vector2i) (Position * UserInterfaceManager.UIScale);

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

        /// <summary>
        ///     Horizontal size flags for container layout.
        /// </summary>
        [ViewVariables]
        [Obsolete("Use HorizontalAlignment and HorizontalExpand instead.")]
        public SizeFlags SizeFlagsHorizontal
        {
            get
            {
                var flags = HorizontalAlignment switch
                {
                    HAlignment.Stretch => SizeFlags.Fill,
                    HAlignment.Left => SizeFlags.None,
                    HAlignment.Center => SizeFlags.ShrinkCenter,
                    HAlignment.Right => SizeFlags.ShrinkEnd,
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (_horizontalExpand)
                    flags |= SizeFlags.Expand;

                return flags;
            }
            set
            {
                HorizontalExpand = (value & SizeFlags.Expand) != 0;
                HorizontalAlignment = (value & ~SizeFlags.Expand) switch
                {
                    SizeFlags.None => HAlignment.Left,
                    SizeFlags.Fill => HAlignment.Stretch,
                    SizeFlags.ShrinkCenter => HAlignment.Center,
                    SizeFlags.ShrinkEnd => HAlignment.Right,
                    _ => throw new ArgumentOutOfRangeException()
                };

                Parent?.UpdateLayout();
            }
        }

        /// <summary>
        ///     Vertical size flags for container layout.
        /// </summary>
        [Obsolete("Use VerticalAlignment and VerticalExpand instead.")]
        [ViewVariables]
        public SizeFlags SizeFlagsVertical
        {
            get
            {
                var flags = _verticalAlignment switch
                {
                    VAlignment.Stretch => SizeFlags.Fill,
                    VAlignment.Top => SizeFlags.None,
                    VAlignment.Center => SizeFlags.ShrinkCenter,
                    VAlignment.Bottom => SizeFlags.ShrinkEnd,
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (_verticalExpand)
                    flags |= SizeFlags.Expand;

                return flags;
            }
            set
            {
                VerticalExpand = (value & SizeFlags.Expand) != 0;
                VerticalAlignment = (value & ~SizeFlags.Expand) switch
                {
                    SizeFlags.None => VAlignment.Top,
                    SizeFlags.Fill => VAlignment.Stretch,
                    SizeFlags.ShrinkCenter => VAlignment.Center,
                    SizeFlags.ShrinkEnd => VAlignment.Bottom,
                    _ => throw new ArgumentOutOfRangeException()
                };

                Parent?.UpdateLayout();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public HAlignment HorizontalAlignment
        {
            get => _horizontalAlignment;
            set
            {
                _horizontalAlignment = value;
                InvalidateArrange();
            }
        }


        [ViewVariables(VVAccess.ReadWrite)]
        public VAlignment VerticalAlignment
        {
            get => _verticalAlignment;
            set
            {
                _verticalAlignment = value;
                InvalidateArrange();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool HorizontalExpand
        {
            get => _horizontalExpand;
            set
            {
                _horizontalExpand = value;
                Parent?.InvalidateMeasure();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool VerticalExpand
        {
            get => _verticalExpand;
            set
            {
                _verticalExpand = value;
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

                Parent?.UpdateLayout();
            }
        }

        /// <summary>
        ///     A combination of <see cref="CustomMinimumSize" /> and <see cref="CalculateMinimumSize" />,
        ///     Whichever is greater.
        ///     Use this for whenever you need the *actual* minimum size of something.
        /// </summary>
        /// <remarks>
        ///     This is in virtual pixels.
        /// </remarks>
        /// <seealso cref="CombinedPixelMinimumSize"/>
        [Obsolete("Use DesiredSize and Measure()")]
        public Vector2 CombinedMinimumSize => DesiredSize;

        /// <summary>
        ///     The <see cref="CombinedMinimumSize"/>, in physical pixels.
        /// </summary>
        [Obsolete("Use DesiredSize and Measure()")]
        public Vector2i CombinedPixelMinimumSize => (Vector2i) (CombinedMinimumSize * UIScale);

        /// <summary>
        ///     A custom minimum size. If the control-calculated size is is smaller than this, this is used instead.
        /// </summary>
        /// <seealso cref="CalculateMinimumSize" />
        /// <seealso cref="CombinedMinimumSize" />
        [ViewVariables]
        public Vector2 CustomMinimumSize
        {
            get => (_minWidth, _minHeight);
            set => (MinWidth, MinHeight) = Vector2.ComponentMax(Vector2.Zero, value);
        }

        public Vector2 SetSize
        {
            get => (_setWidth, _setHeight);
            set => (SetWidth, SetHeight) = value;
        }

        public Vector2 MaxSize
        {
            get => (_maxWidth, _maxHeight);
            set => (MaxWidth, MaxHeight) = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float MinWidth
        {
            get => _minWidth;
            set
            {
                _minWidth = value;
                InvalidateMeasure();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float MinHeight
        {
            get => _minHeight;
            set
            {
                _minHeight = value;
                InvalidateMeasure();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float SetWidth
        {
            get => _setWidth;
            set
            {
                _setWidth = value;
                InvalidateMeasure();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float SetHeight
        {
            get => _setHeight;
            set
            {
                _setHeight = value;
                InvalidateMeasure();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxWidth
        {
            get => _maxWidth;
            set
            {
                _maxWidth = value;
                InvalidateMeasure();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxHeight
        {
            get => _maxHeight;
            set
            {
                _maxHeight = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     Override this to calculate a minimum size for this control.
        ///     Do NOT call this directly to get the minimum size for layout purposes!
        ///     Use <see cref="CombinedMinimumSize" /> for the ACTUAL minimum size.
        /// </summary>
        [Obsolete("Implement MeasureOverride instead")]
        protected virtual Vector2 CalculateMinimumSize()
        {
            return Vector2.Zero;
        }

        /// <summary>
        ///     Tells the GUI system that the minimum size of this control may have changed,
        ///     so that say containers will re-sort it if necessary.
        /// </summary>
        [Obsolete("Use InvalidateMeasure()")]
        public void MinimumSizeChanged()
        {
            InvalidateMeasure();
        }

        public void InvalidateMeasure()
        {
            if (!IsMeasureValid)
                return;

            IsMeasureValid = false;
            IsArrangeValid = false;

            UserInterfaceManagerInternal.QueueMeasureUpdate(this);
        }

        /// <summary>
        ///     Forces this component to immediately calculate layout.
        /// </summary>
        /// <remarks>
        ///     This should only be used for unit testing,
        ///     where running the deferred layout updating system in the UI manager can be annoying.
        ///     If you are forced to use this in regular code, you have found a bug.
        /// </remarks>
        public void ForceRunLayoutUpdate()
        {
            // TODO: Fix or remove this
            if (PreviousArrange.HasValue)
                Arrange(PreviousArrange.Value);
        }

        public void InvalidateArrange()
        {
            if (!IsArrangeValid)
            {
                // Already queued for a layout update, don't bother.
                return;
            }

            IsArrangeValid = false;
            UserInterfaceManagerInternal.QueueArrangeUpdate(this);
        }

        [Obsolete("Use InvalidateArrange()")]
        protected void UpdateLayout()
        {
            InvalidateArrange();
        }

        public void Measure(Vector2 availableSize)
        {
            if (!IsMeasureValid || PreviousMeasure != availableSize)
            {
                IsMeasureValid = true;
                var desired = MeasureCore(availableSize);

                if (desired.X < 0 || desired.Y < 0 || !float.IsFinite(desired.X) || !float.IsFinite(desired.Y))
                    throw new InvalidOperationException("Invalid size returned from Measure()");

                var prev = DesiredSize;
                DesiredSize = desired;
                PreviousMeasure = availableSize;

                if (prev != desired && Parent != null && !Parent._measuring)
                    Parent?.InvalidateMeasure();
            }
        }

        protected virtual Vector2 MeasureCore(Vector2 availableSize)
        {
            if (!Visible)
                return default;

            if (_stylingDirty)
                ForceRunStyleUpdate();

            var withoutMargin = _margin.Deflate(availableSize);

            var constrained = ApplySizeConstraints(this, withoutMargin);

            Vector2 measured;
            try
            {
                _measuring = true;
                measured = Vector2.ComponentMax(
                    MeasureOverride(constrained),
                    // For the time being keep the old CalculateMinimumSize around.
#pragma warning disable 618
                    CalculateMinimumSize());
#pragma warning restore 618
            }
            finally
            {
                _measuring = false;
            }

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
            return Vector2.ComponentMin(measured, availableSize);
        }

        protected virtual Vector2 MeasureOverride(Vector2 availableSize)
        {
            var min = Vector2.Zero;

            foreach (var child in Children)
            {
                child.Measure(availableSize);
                min = Vector2.ComponentMax(min, child.DesiredSize);
            }

            return min;
        }

        public void ArrangePixel(UIBox2i finalRect)
        {
            var topLeft = finalRect.TopLeft / UIScale;
            var bottomRight = finalRect.BottomRight / UIScale;

            Arrange(new UIBox2(topLeft, bottomRight));
        }

        public void Arrange(UIBox2 finalRect)
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

        protected virtual void ArrangeCore(UIBox2 finalRect)
        {
            if (!Visible)
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

            Size = size;
            _isLayoutUpdateOverrideUsed = true;
#pragma warning disable 618
            LayoutUpdateOverride();
#pragma warning restore 618
            if (!_isLayoutUpdateOverrideUsed)
            {
                var arranged = ArrangeOverride(size);

                size = Vector2.ComponentMin(arranged, size);
            }

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

        protected virtual Vector2 ArrangeOverride(Vector2 finalSize)
        {
            foreach (var child in Children)
            {
                child.Arrange(UIBox2.FromDimensions(Vector2.Zero, finalSize));
            }

            return finalSize;
        }

        [Obsolete("Use Control.ArrangePixel")]
        protected void FitChildInPixelBox(Control child, UIBox2i pixelBox)
        {
            child.ArrangePixel(pixelBox);
        }

        [Obsolete("Use Control.Arrange")]
        protected void FitChildInBox(Control child, UIBox2 box)
        {
            child.Arrange(box);
        }

        [Obsolete("Implement ArrangeOverride instead.")]
        protected virtual void LayoutUpdateOverride()
        {
            _isLayoutUpdateOverrideUsed = false;
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
            minH = MathHelper.Clamp(minW, minConstraint, minH);

            return (
                Math.Clamp(avail.X, minW, maxW),
                Math.Clamp(avail.Y, minH, maxH));
        }

        /// <summary>
        ///     Controls how a control changes size when inside a container.
        /// </summary>
        [Flags]
        [PublicAPI]
        public enum SizeFlags : byte
        {
            /// <summary>
            ///     Shrink to the begin of the specified axis.
            /// </summary>
            None = 0,

            /// <summary>
            ///     Fill as much space as possible in a container, without pushing others.
            /// </summary>
            Fill = 1,

            /// <summary>
            ///     Fill as much space as possible in a container, pushing other nodes.
            ///     The ratio of pushing if there's multiple set to expand is dependant on <see cref="SizeFlagsStretchRatio" />
            /// </summary>
            Expand = 2,

            /// <summary>
            ///     Combination of <see cref="Fill" /> and <see cref="Expand" />.
            /// </summary>
            FillExpand = 3,

            /// <summary>
            ///     Shrink inside a container, aligning to the center.
            /// </summary>
            ShrinkCenter = 4,

            /// <summary>
            ///     Shrink inside a container, aligning to the end.
            /// </summary>
            ShrinkEnd = 8,
        }

        public enum HAlignment
        {
            Stretch,
            Left,
            Center,
            Right
        }

        public enum VAlignment
        {
            Stretch,
            Top,
            Center,
            Bottom
        }
    }
}
