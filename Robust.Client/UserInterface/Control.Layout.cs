using System;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface
{
    public partial class Control
    {
        public event Action<Control>? OnMinimumSizeChanged;

        private Vector2 _size;

        private float _sizeFlagsStretchRatio = 1;
        private Vector2? _calculatedMinimumSize;
        private Vector2 _customMinimumSize;
        private SizeFlags _sizeFlagsHorizontal = SizeFlags.Fill;
        private SizeFlags _sizeFlagsVertical = SizeFlags.Fill;
        private bool _layoutDirty;

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
                UpdateLayout();
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
        public SizeFlags SizeFlagsHorizontal
        {
            get => _sizeFlagsHorizontal;
            set
            {
                _sizeFlagsHorizontal = value;

                Parent?.UpdateLayout();
            }
        }

        /// <summary>
        ///     Vertical size flags for container layout.
        /// </summary>
        [ViewVariables]
        public SizeFlags SizeFlagsVertical
        {
            get => _sizeFlagsVertical;
            set
            {
                _sizeFlagsVertical = value;

                Parent?.UpdateLayout();
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
        [ViewVariables]
        public Vector2 CombinedMinimumSize
        {
            get
            {
                if (!_calculatedMinimumSize.HasValue)
                {
                    _updateMinimumSize();
                    DebugTools.Assert(_calculatedMinimumSize.HasValue);
                }

                return Vector2.ComponentMax(CustomMinimumSize, _calculatedMinimumSize!.Value);
            }
        }

        /// <summary>
        ///     The <see cref="CombinedMinimumSize"/>, in physical pixels.
        /// </summary>
        public Vector2i CombinedPixelMinimumSize => (Vector2i) (CombinedMinimumSize * UIScale);

        /// <summary>
        ///     A custom minimum size. If the control-calculated size is is smaller than this, this is used instead.
        /// </summary>
        /// <seealso cref="CalculateMinimumSize" />
        /// <seealso cref="CombinedMinimumSize" />
        [ViewVariables]
        public Vector2 CustomMinimumSize
        {
            get => _customMinimumSize;
            set
            {
                _customMinimumSize = Vector2.ComponentMax(Vector2.Zero, value);
                MinimumSizeChanged();
            }
        }

        private void _updateMinimumSize()
        {
            if (_stylingDirty)
            {
                ForceRunStyleUpdate();
            }

            _calculatedMinimumSize = Vector2.ComponentMax(Vector2.Zero, CalculateMinimumSize());
        }

        /// <summary>
        ///     Override this to calculate a minimum size for this control.
        ///     Do NOT call this directly to get the minimum size for layout purposes!
        ///     Use <see cref="CombinedMinimumSize" /> for the ACTUAL minimum size.
        /// </summary>
        protected virtual Vector2 CalculateMinimumSize()
        {
            var min = Vector2.Zero;
            foreach (var child in Children)
            {
                min = Vector2.ComponentMax(min, child.CombinedMinimumSize);
            }
            return min;
        }

        /// <summary>
        ///     Tells the GUI system that the minimum size of this control may have changed,
        ///     so that say containers will re-sort it if necessary.
        /// </summary>
        public void MinimumSizeChanged()
        {
            _calculatedMinimumSize = null;
            OnMinimumSizeChanged?.Invoke(this);

            Parent?.MinimumSizeChanged();
            UpdateLayout();
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
            DoLayoutUpdate();

            foreach (var child in Children)
            {
                child.ForceRunLayoutUpdate();
            }
        }

        protected void UpdateLayout()
        {
            if (_layoutDirty)
            {
                // Already queued for a layout update, don't bother.
                return;
            }

            _layoutDirty = true;
            UserInterfaceManagerInternal.QueueLayoutUpdate(this);
        }

        protected void FitChildInPixelBox(Control child, UIBox2i pixelBox)
        {
            var topLeft = pixelBox.TopLeft / UIScale;
            var bottomRight = pixelBox.BottomRight / UIScale;

            FitChildInBox(child, new UIBox2(topLeft, bottomRight));
        }

        protected void FitChildInBox(Control child, UIBox2 box)
        {
            DebugTools.Assert(child.Parent == this);

            var (minX, minY) = child.CombinedMinimumSize;
            var newPosX = box.Left;
            var newSizeX = minX;

            if ((child.SizeFlagsHorizontal & SizeFlags.ShrinkEnd) != 0)
            {
                newPosX += (box.Width - minX);
            }
            else if ((child.SizeFlagsHorizontal & SizeFlags.ShrinkCenter) != 0)
            {
                newPosX += (box.Width - minX) / 2;
            }
            else if ((child.SizeFlagsHorizontal & SizeFlags.Fill) != 0)
            {
                newSizeX = Math.Max(box.Width, newSizeX);
            }

            var newPosY = box.Top;
            var newSizeY = minY;

            if ((child.SizeFlagsVertical & SizeFlags.ShrinkEnd) != 0)
            {
                newPosY += (box.Height - minY);
            }
            else if ((child.SizeFlagsVertical & SizeFlags.ShrinkCenter) != 0)
            {
                newPosY += (box.Height - minY) / 2;
            }
            else if ((child.SizeFlagsVertical & SizeFlags.Fill) != 0)
            {
                newSizeY = Math.Max(box.Height, newSizeY);
            }

            child.Position = new Vector2(newPosX, newPosY);
            child.Size = new Vector2(newSizeX, newSizeY);
        }

        internal void DoLayoutUpdate()
        {
            LayoutUpdateOverride();
            _layoutDirty = false;
        }

        protected virtual void LayoutUpdateOverride()
        {
            foreach (var child in Children)
            {
                FitChildInPixelBox(child, PixelSizeBox);
            }
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
    }
}
