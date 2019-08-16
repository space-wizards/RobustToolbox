using System;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface
{
    public partial class Control
    {
        private float _anchorBottom;
        private float _anchorLeft;
        private float _anchorRight;
        private float _anchorTop;
        private float _marginRight;
        private float _marginLeft;
        private float _marginTop;
        private float _marginBottom;
        private Vector2 _position;
        private Vector2 _sizeByMargins;
        private Vector2 _size;
        private float _sizeFlagsStretchRatio = 1;
        private Vector2? _calculatedMinimumSize;
        private Vector2 _customMinimumSize;
        private GrowDirection _growHorizontal;
        private GrowDirection _growVertical;
        public event Action<Control> OnMinimumSizeChanged;
        private SizeFlags _sizeFlagsHorizontal = SizeFlags.Fill;
        private SizeFlags _sizeFlagsVertical = SizeFlags.Fill;
        private bool _layoutDirty;

        /// <summary>
        ///     The value of an anchor that is exactly on the begin of the parent control.
        /// </summary>
        public const float AnchorBegin = 0;

        /// <summary>
        ///     The value of an anchor that is exactly on the end of the parent control.
        /// </summary>
        public const float AnchorEnd = 1;

        /// <summary>
        ///     Specifies the anchor of the bottom edge of the control.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float AnchorBottom
        {
            get => _anchorBottom;
            set
            {
                _anchorBottom = value;
                DoLayoutUpdate();
            }
        }

        /// <summary>
        ///     Specifies the anchor of the left edge of the control.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float AnchorLeft
        {
            get => _anchorLeft;
            set
            {
                _anchorLeft = value;
                DoLayoutUpdate();
            }
        }

        /// <summary>
        ///     Specifies the anchor of the right edge of the control.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float AnchorRight
        {
            get => _anchorRight;
            set
            {
                _anchorRight = value;
                DoLayoutUpdate();
            }
        }

        /// <summary>
        ///     Specifies the anchor of the top edge of the control.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float AnchorTop
        {
            get => _anchorTop;
            set
            {
                _anchorTop = value;
                DoLayoutUpdate();
            }
        }

        /// <summary>
        ///     Specifies the margin of the right edge of the control.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MarginRight
        {
            get => _marginRight;
            set
            {
                _marginRight = value;
                DoLayoutUpdate();
            }
        }

        /// <summary>
        ///     Specifies the margin of the left edge of the control.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MarginLeft
        {
            get => _marginLeft;
            set
            {
                _marginLeft = value;
                DoLayoutUpdate();
            }
        }

        /// <summary>
        ///     Specifies the margin of the top edge of the control.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MarginTop
        {
            get => _marginTop;
            set
            {
                _marginTop = value;
                DoLayoutUpdate();
            }
        }

        /// <summary>
        ///     Specifies the margin of the bottom edge of the control.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MarginBottom
        {
            get => _marginBottom;
            set
            {
                _marginBottom = value;
                DoLayoutUpdate();
            }
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
            set
            {
                var (diffX, diffY) = value - _sizeByMargins;
                _marginRight += diffX;
                _marginBottom += diffY;
                DoLayoutUpdate();
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
        public UIBox2 SizeBox => new UIBox2(Vector2.Zero, Size);

        /// <summary>
        ///     A <see cref="UIBox2i"/> with the top left at 0,0 and the size equal to <see cref="PixelSize"/>.
        /// </summary>
        /// <seealso cref="SizeBox"/>
        public UIBox2i PixelSizeBox => new UIBox2i(Vector2i.Zero, PixelSize);

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
        public Vector2 Position
        {
            get => _position;
            set
            {
                var (diffX, diffY) = value - _position;
                _marginTop += diffY;
                _marginBottom += diffY;
                _marginLeft += diffX;
                _marginRight += diffX;
                DoLayoutUpdate();
            }
        }

        /// <summary>
        ///     The position of the top left corner of the control, in physical pixels.
        /// </summary>
        /// <seealso cref="Position"/>
        [ViewVariables]
        public Vector2i PixelPosition => (Vector2i) (_position * UserInterfaceManager.UIScale);

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
        public UIBox2 Rect => UIBox2.FromDimensions(_position, _size);

        /// <summary>
        ///     Represents the "rectangle" of the control relative to the parent, in physical pixels.
        /// </summary>
        /// <seealso cref="Rect"/>
        public UIBox2i PixelRect => UIBox2i.FromDimensions(PixelPosition, PixelSize);

        /// <summary>
        ///     Determines how the control will move on the horizontal axis to ensure it is at its minimum size.
        ///     See <see cref="GrowDirection"/> for more information.
        /// </summary>
        [ViewVariables]
        public GrowDirection GrowHorizontal
        {
            get => _growHorizontal;
            set
            {
                _growHorizontal = value;
                UpdateLayout();
            }
        }

        /// <summary>
        ///     Determines how the control will move on the vertical axis to ensure it is at its minimum size.
        ///     See <see cref="GrowDirection"/> for more information.
        /// </summary>
        [ViewVariables]
        public GrowDirection GrowVertical
        {
            get => _growVertical;
            set
            {
                _growVertical = value;
                UpdateLayout();
            }
        }

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

                if (Parent is Container container)
                {
                    container.QueueSortChildren();
                }
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

                if (Parent is Container container)
                {
                    container.QueueSortChildren();
                }
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
                if (Parent is Container container)
                {
                    container.QueueSortChildren();
                }
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

                return Vector2.ComponentMax(CustomMinimumSize, _calculatedMinimumSize.Value);
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
            _calculatedMinimumSize = Vector2.ComponentMax(Vector2.Zero, CalculateMinimumSize());
        }

        /// <summary>
        ///     Override this to calculate a minimum size for this control.
        ///     Do NOT call this directly to get the minimum size for layout purposes!
        ///     Use <see cref="CombinedMinimumSize" /> for the ACTUAL minimum size.
        /// </summary>
        protected virtual Vector2 CalculateMinimumSize()
        {
            return Vector2.Zero;
        }

        /// <summary>
        ///     Tells the GUI system that the minimum size of this control may have changed,
        ///     so that say containers will re-sort it if necessary.
        /// </summary>
        public void MinimumSizeChanged()
        {
            _calculatedMinimumSize = null;
            OnMinimumSizeChanged?.Invoke(this);
        }

        /// <summary>
        ///     Sets an anchor AND a margin preset. This is most likely the method you want.
        /// </summary>
        public void SetAnchorAndMarginPreset(LayoutPreset preset, LayoutPresetMode mode = LayoutPresetMode.MinSize,
            int margin = 0)
        {
            SetAnchorPreset(preset);
            SetMarginsPreset(preset, mode, margin);
        }

        /// <summary>
        ///     Changes all the anchors of a node at once to common presets.
        ///     The result is that the anchors are laid out to be suitable for a preset.
        /// </summary>
        /// <param name="preset">
        ///     The preset to apply to the anchors.
        /// </param>
        /// <param name="keepMargin">
        ///     If this is true, the control margin values themselves will not be changed,
        ///     and the control position and size will change according to the new anchor parameters.
        ///     If false, the control margins will adjust so that the control position and size remains the same relative to its parent.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if <paramref name="preset" /> isn't a valid preset value.
        /// </exception>
        public void SetAnchorPreset(LayoutPreset preset, bool keepMargin = false)
        {
            // TODO: Implement keepMargin.

            // Left Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.CenterLeft:
                case LayoutPreset.LeftWide:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.Wide:
                case LayoutPreset.TopWide:
                case LayoutPreset.BottomWide:
                    AnchorLeft = 0;
                    break;
                case LayoutPreset.CenterTop:
                case LayoutPreset.CenterBottom:
                case LayoutPreset.Center:
                case LayoutPreset.VerticalCenterWide:
                    AnchorLeft = 0.5f;
                    break;
                case LayoutPreset.TopRight:
                case LayoutPreset.BottomRight:
                case LayoutPreset.CenterRight:
                case LayoutPreset.RightWide:
                    AnchorLeft = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Top Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.TopRight:
                case LayoutPreset.LeftWide:
                case LayoutPreset.TopWide:
                case LayoutPreset.Wide:
                case LayoutPreset.RightWide:
                case LayoutPreset.CenterTop:
                case LayoutPreset.VerticalCenterWide:
                    AnchorTop = 0;
                    break;
                case LayoutPreset.CenterLeft:
                case LayoutPreset.CenterRight:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.Center:
                    AnchorTop = 0.5f;
                    break;
                case LayoutPreset.CenterBottom:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.BottomRight:
                case LayoutPreset.BottomWide:
                    AnchorTop = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Right Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.CenterLeft:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.LeftWide:
                    AnchorRight = 0;
                    break;
                case LayoutPreset.CenterTop:
                case LayoutPreset.CenterBottom:
                case LayoutPreset.Center:
                case LayoutPreset.VerticalCenterWide:
                    AnchorRight = 0.5f;
                    break;
                case LayoutPreset.CenterRight:
                case LayoutPreset.TopRight:
                case LayoutPreset.Wide:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.TopWide:
                case LayoutPreset.BottomWide:
                case LayoutPreset.RightWide:
                case LayoutPreset.BottomRight:
                    AnchorRight = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Bottom Anchor.
            switch (preset)
            {
                case LayoutPreset.TopWide:
                case LayoutPreset.TopLeft:
                case LayoutPreset.TopRight:
                case LayoutPreset.CenterTop:
                    AnchorBottom = 0;
                    break;
                case LayoutPreset.CenterLeft:
                case LayoutPreset.CenterRight:
                case LayoutPreset.Center:
                case LayoutPreset.HorizontalCenterWide:
                    AnchorBottom = 0.5f;
                    break;
                case LayoutPreset.CenterBottom:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.BottomRight:
                case LayoutPreset.LeftWide:
                case LayoutPreset.Wide:
                case LayoutPreset.RightWide:
                case LayoutPreset.VerticalCenterWide:
                case LayoutPreset.BottomWide:
                    AnchorBottom = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }
        }

        /// <summary>
        ///     Changes all the margins of a control at once to common presets.
        ///     The result is that the control is laid out as specified by the preset.
        /// </summary>
        /// <param name="preset"></param>
        /// <param name="resizeMode"></param>
        /// <param name="margin">Some extra margin to add depending on the preset chosen.</param>
        public void SetMarginsPreset(LayoutPreset preset, LayoutPresetMode resizeMode = LayoutPresetMode.MinSize,
            int margin = 0)
        {
            var newSize = Size;
            var minSize = CombinedMinimumSize;
            if ((resizeMode & LayoutPresetMode.KeepWidth) == 0)
            {
                newSize = new Vector2(minSize.X, newSize.Y);
            }

            if ((resizeMode & LayoutPresetMode.KeepHeight) == 0)
            {
                newSize = new Vector2(newSize.X, minSize.Y);
            }

            var parentSize = Parent?.Size ?? Vector2.Zero;

            // Left Margin.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.CenterLeft:
                case LayoutPreset.LeftWide:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.Wide:
                case LayoutPreset.TopWide:
                case LayoutPreset.BottomWide:
                    // The AnchorLeft bit is to reverse the effect of anchors,
                    // So that the preset result is the same no matter what margins are set.
                    _marginLeft = parentSize.X * (0 - AnchorLeft) + margin;
                    break;
                case LayoutPreset.CenterTop:
                case LayoutPreset.CenterBottom:
                case LayoutPreset.Center:
                case LayoutPreset.VerticalCenterWide:
                    _marginLeft = parentSize.X * (0.5f - AnchorLeft) - newSize.X / 2;
                    break;
                case LayoutPreset.TopRight:
                case LayoutPreset.BottomRight:
                case LayoutPreset.CenterRight:
                case LayoutPreset.RightWide:
                    _marginLeft = parentSize.X * (1 - AnchorLeft) - newSize.X - margin;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Top Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.TopRight:
                case LayoutPreset.LeftWide:
                case LayoutPreset.TopWide:
                case LayoutPreset.Wide:
                case LayoutPreset.RightWide:
                case LayoutPreset.CenterTop:
                case LayoutPreset.VerticalCenterWide:
                    _marginTop = parentSize.Y * (0 - AnchorTop) + margin;
                    break;
                case LayoutPreset.CenterLeft:
                case LayoutPreset.CenterRight:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.Center:
                    _marginTop = parentSize.Y * (0.5f - AnchorTop) - newSize.Y / 2;
                    break;
                case LayoutPreset.CenterBottom:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.BottomRight:
                case LayoutPreset.BottomWide:
                    _marginTop = parentSize.Y * (1 - AnchorTop) - newSize.Y - margin;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Right Anchor.
            switch (preset)
            {
                case LayoutPreset.TopLeft:
                case LayoutPreset.CenterLeft:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.LeftWide:
                    _marginRight = parentSize.X * (0 - AnchorRight) + newSize.X + margin;
                    break;
                case LayoutPreset.CenterTop:
                case LayoutPreset.CenterBottom:
                case LayoutPreset.Center:
                case LayoutPreset.VerticalCenterWide:
                    _marginRight = parentSize.X * (0.5f - AnchorRight) + newSize.X;
                    break;
                case LayoutPreset.CenterRight:
                case LayoutPreset.TopRight:
                case LayoutPreset.Wide:
                case LayoutPreset.HorizontalCenterWide:
                case LayoutPreset.TopWide:
                case LayoutPreset.BottomWide:
                case LayoutPreset.RightWide:
                case LayoutPreset.BottomRight:
                    _marginRight = parentSize.X * (1 - AnchorRight) - margin;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            // Bottom Anchor.
            switch (preset)
            {
                case LayoutPreset.TopWide:
                case LayoutPreset.TopLeft:
                case LayoutPreset.TopRight:
                case LayoutPreset.CenterTop:
                    _marginBottom = parentSize.Y * (0 - AnchorBottom) + newSize.Y + margin;
                    break;
                case LayoutPreset.CenterLeft:
                case LayoutPreset.CenterRight:
                case LayoutPreset.Center:
                case LayoutPreset.HorizontalCenterWide:
                    _marginBottom = parentSize.Y * (0.5f - AnchorBottom) + newSize.Y;
                    break;
                case LayoutPreset.CenterBottom:
                case LayoutPreset.BottomLeft:
                case LayoutPreset.BottomRight:
                case LayoutPreset.LeftWide:
                case LayoutPreset.Wide:
                case LayoutPreset.RightWide:
                case LayoutPreset.VerticalCenterWide:
                case LayoutPreset.BottomWide:
                    _marginBottom = parentSize.Y * (1 - AnchorBottom) - margin;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }

            UpdateLayout();
        }

        public void ForceRunLayoutUpdate()
        {
            DoLayoutUpdate();

            // I already apologize, Sonar.
            // This is terrible.
            if (this is Container container)
            {
                container.SortChildren();
            }

            foreach (var child in Children)
            {
                child.ForceRunLayoutUpdate();
            }
        }

        public enum LayoutPreset : byte
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3,
            CenterLeft = 4,
            CenterTop = 5,
            CenterRight = 6,
            CenterBottom = 7,
            Center = 8,
            LeftWide = 9,
            TopWide = 10,
            RightWide = 11,
            BottomWide = 12,
            VerticalCenterWide = 13,
            HorizontalCenterWide = 14,
            Wide = 15,
        }

        /// <seealso cref="Control.SetMarginsPreset" />
        [Flags]
        [PublicAPI]
        public enum LayoutPresetMode : byte
        {
            /// <summary>
            ///     Reset control size to minimum size.
            /// </summary>
            MinSize = 0,

            /// <summary>
            ///     Reset height to minimum but keep width the same.
            /// </summary>
            KeepWidth = 1,

            /// <summary>
            ///     Reset width to minimum but keep height the same.
            /// </summary>
            KeepHeight = 2,

            /// <summary>
            ///     Do not modify control size at all.
            /// </summary>
            KeepSize = KeepWidth | KeepHeight,
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

        /// <summary>
        ///     Controls how the control should move when its wanted size (controlled by anchors/margins) is smaller
        ///     than its minimum size.
        /// </summary>
        public enum GrowDirection : byte
        {
            /// <summary>
            ///     The control will expand to the bottom right to reach its minimum size.
            /// </summary>
            End = 0,

            /// <summary>
            ///     The control will expand to the top left to reach its minimum size.
            /// </summary>
            Begin,

            /// <summary>
            ///     The control will expand on all axes equally to reach its minimum size.
            /// </summary>
            Both
        }

        private protected void UpdateLayout()
        {
            if (_layoutDirty)
            {
                // Already queued for a layout update, don't bother.
                return;
            }

            _layoutDirty = true;
            UserInterfaceManagerInternal.QueueLayoutUpdate(this);
        }

        internal void DoLayoutUpdate()
        {
            _layoutDirty = false;
            var (pSizeX, pSizeY) = Parent?._size ?? Vector2.Zero;

            // Calculate where the control "wants" to be by its anchors/margins.
            var top = _anchorTop * pSizeY + _marginTop;
            var left = _anchorLeft * pSizeX + _marginLeft;
            var right = _anchorRight * pSizeX + _marginRight;
            var bottom = _anchorBottom * pSizeY + _marginBottom;

            // The position we want.
            var (wPosX, wPosY) = (left, top);
            // The size we want.
            var (wSizeX, wSizeY) = (right - left, bottom - top);
            var (minSizeX, minSizeY) = CombinedMinimumSize;

            _handleLayoutOverflow(GrowHorizontal, minSizeX, wPosX, wSizeX, out var posX, out var sizeX);
            _handleLayoutOverflow(GrowVertical, minSizeY, wPosY, wSizeY, out var posY, out var sizeY);

            var oldSize = _size;
            _position = (posX, posY);
            _size = (sizeX, sizeY);
            _sizeByMargins = (wSizeX, wSizeY);

            // If size is different then child controls may need to be laid out differently.
            if (_size != oldSize)
            {
                Resized();

                foreach (var child in _orderedChildren)
                {
                    child.UpdateLayout();
                }
            }
        }

        private static void _handleLayoutOverflow(GrowDirection direction, float minSize, float wPos, float wSize,
            out float pos,
            out float size)
        {
            var overflow = minSize - wSize;
            if (overflow <= 0)
            {
                pos = wPos;
                size = wSize;
                return;
            }

            switch (direction)
            {
                case GrowDirection.End:
                    pos = wPos;
                    break;
                case GrowDirection.Begin:
                    pos = wPos - overflow;
                    break;
                case GrowDirection.Both:
                    pos = wPos - overflow / 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            size = minSize;
        }
    }
}
