using System;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class SplitContainer : Container
    {
        /// <summary>
        /// Defines how user-initiated moving of the split should work. See documentation
        /// for each enum value to see how the different options work.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public SplitResizeMode ResizeMode { get; set; }

        /// <summary>
        /// Width of the split in virtual pixels
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SplitWidth
        {
            get => _splitWidth;
            set
            {
                _splitWidth = value;
                InvalidateMeasure();
            }
        }

        private float _splitWidth;

        /// <summary>
        ///     This width determines the minimum size of the draggable area around the split. This has no effect if it
        ///     is smaller than <see cref="SplitWidth"/>, which determines the visual padding/width.
        /// </summary>
        public float MinDraggableWidth = 10f;

        /// <summary>
        /// Virtual pixel offset from the edge beyond which the split cannot be moved.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SplitEdgeSeparation { get; set; }

        private float _splitStart;

        /// <summary>
        /// Virtual pixel offset from the center of the split.
        /// </summary>
        public float SplitCenter
        {
            get => _splitStart + _splitWidth / 2;
            set
            {
                State = SplitState.Manual;
                _splitStart = value - _splitWidth / 2;
                ClampSplitCenter();
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Virtual pixel fraction of the split.
        /// </summary>
        /// <value>Takes a float from 0 to 1.</value>
        public float SplitFraction
        {
            get
            {
                var size = Vertical ? Size.Y : Size.X;
                if (size == 0)
                    return 0;

                return SplitCenter / size;
            }
            set
            {
                SplitCenter = value * (Vertical ? Size.Y : Size.X);
            }
        }

        private SplitState _splitState;
        private bool _dragging;
        private SplitOrientation _orientation;
        private SplitStretchDirection _stretchDirection;

        private bool Vertical => Orientation == SplitOrientation.Vertical;

        /// <summary>
        /// Whether the split position should be set manually or automatically.
        /// </summary>
        public SplitState State
        {
            get => _splitState;
            set
            {
                _splitState = value;
                InvalidateMeasure();
            }
        }
        
        /// <summary>
        /// Determines which side of the split expands when the parent is resized.
        /// </summary>
        public SplitStretchDirection StretchDirection 
        {
            get => _stretchDirection;
            set
            {
                _stretchDirection = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Whether the split is horizontal or vertical.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public SplitOrientation Orientation
        {
            get => _orientation;
            set
            {
                _orientation = value;
                InvalidateMeasure();
            }
        }

        public SplitContainer()
        {
            MouseFilter = MouseFilterMode.Stop;
            _splitState = SplitState.Auto;
            _stretchDirection = SplitStretchDirection.BottomRight;
            _dragging = false;
            ResizeMode = SplitResizeMode.RespectChildrenMinSize;
            SplitWidth = 10;
            SplitEdgeSeparation = 10;
        }

        protected internal override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);

            if (ResizeMode == SplitResizeMode.NotResizable) return;

            if (_dragging)
            {
                var newOffset = Vertical ? args.RelativePosition.Y : args.RelativePosition.X;

                SplitCenter = newOffset;
                DefaultCursorShape = Vertical ? CursorShape.VResize : CursorShape.HResize;
            }
            else
            {
                // on mouseover, check if they are over the split and change the cursor accordingly
                var cursor = CursorShape.Arrow;
                if (CanDragAt(args.RelativePosition))
                {
                    cursor = Vertical ? CursorShape.VResize : CursorShape.HResize;
                }

                DefaultCursorShape = cursor;
            }
        }


        protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (ResizeMode == SplitResizeMode.NotResizable) return;

            if (_dragging || args.Function != EngineKeyFunctions.UIClick) return;

            if (CanDragAt(args.RelativePosition))
            {
                _dragging = true;
                _splitState = SplitState.Manual;
            }
        }

        protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (args.Function != EngineKeyFunctions.UIClick) return;

            _dragging = false;
            DefaultCursorShape = CursorShape.Arrow;
        }

        private bool CanDragAt(Vector2 relativePosition)
        {
            var distance = Vertical
                ? Math.Abs(relativePosition.Y - SplitCenter)
                : Math.Abs(relativePosition.X - SplitCenter);

            return distance <= _splitWidth || distance <= MinDraggableWidth;
        }

        /// <summary>
        /// Ensures the split center is within all necessary limits
        /// </summary>
        /// <param name="firstMinSize">min size of the first child, will calculate if null</param>
        /// <param name="secondMinSize">min size of the second child, will calculate if null</param>
        /// <returns></returns>
        private void ClampSplitCenter(Vector2? desiredSize = null, float? firstMinSize = null, float? secondMinSize = null)
        {
            // min / max x and y extents in relative virtual pixels of where the split can go regardless
            // of anything else.

            var controlSize = desiredSize ?? Size;
            var splitMax = (Vertical ? controlSize.Y : controlSize.X) - _splitWidth - SplitEdgeSeparation;
            var desiredSplit = _splitStart;
            if (desiredSize != null && StretchDirection == SplitStretchDirection.TopLeft)
                desiredSplit += Vertical ? desiredSize.Value.Y - Size.Y : desiredSize.Value.X - Size.X;
            _splitStart = MathHelper.Clamp(desiredSplit, SplitEdgeSeparation, splitMax);

            if (ResizeMode == SplitResizeMode.RespectChildrenMinSize && ChildCount == 2)
            {
                var first = GetChild(0);
                var second = GetChild(1);

                var firstDesiredSize = firstMinSize ?? (Vertical ? first.DesiredSize.Y : first.DesiredSize.X);
                var secondDesiredSize = secondMinSize ??  (Vertical ? second.DesiredSize.Y : second.DesiredSize.X);
                var firstOrientedMinSize = Vertical ? first.MinSize.Y : first.MinSize.X;
                var secondOrientedMinSize = Vertical ? second.MinSize.Y : second.MinSize.X;

                if (firstOrientedMinSize > firstDesiredSize && firstOrientedMinSize != 0)
                {
                    first.Measure(controlSize);
                }

                if (secondOrientedMinSize > secondDesiredSize && secondOrientedMinSize != 0)
                {
                    second.Measure(controlSize);
                }

                firstMinSize = Vertical ? first.DesiredSize.Y : first.DesiredSize.X;
                secondMinSize = Vertical ? second.DesiredSize.Y : second.DesiredSize.X;
                var size = Vertical ? controlSize.Y : controlSize.X;

                _splitStart = MathHelper.Clamp(_splitStart, firstMinSize.Value,
                    size - (secondMinSize.Value + _splitWidth));
            }
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            if (ChildCount != 2)
            {
                return finalSize;
            }

            var first = GetChild(0);
            var second = GetChild(1);

            var firstExpand = Vertical ? first.VerticalExpand : first.HorizontalExpand;
            var secondExpand = Vertical ? second.VerticalExpand : second.HorizontalExpand;

            var firstDesiredSize = Vertical ? first.DesiredSize.Y : first.DesiredSize.X;
            var secondDesiredSize = Vertical ? second.DesiredSize.Y : second.DesiredSize.X;

            var size = Vertical ? finalSize.Y : finalSize.X;

            var ratio = first.SizeFlagsStretchRatio / (first.SizeFlagsStretchRatio + second.SizeFlagsStretchRatio);

            switch (_splitState)
            {
                case SplitState.Manual:
                    // min sizes of children may have changed, ensure the offset still respects the defined limits
                    ClampSplitCenter(finalSize, firstDesiredSize, secondDesiredSize);
                    break;
                case SplitState.Auto:
                {
                    if (firstExpand && secondExpand)
                    {
                        _splitStart = size * ratio - _splitWidth / 2;
                    }
                    else if (firstExpand)
                    {
                        _splitStart = size - secondDesiredSize - _splitWidth;
                    }
                    else if (secondExpand)
                    {
                        _splitStart = firstDesiredSize;
                    }
                    else
                    {
                        ratio = firstDesiredSize + secondDesiredSize <= 0
                            ? 0.5f
                            : firstDesiredSize / (firstDesiredSize + secondDesiredSize);

                        _splitStart = size * ratio - _splitWidth / 2;
                    }

                    _splitStart += MathHelper.Clamp(0f, firstDesiredSize - _splitStart,
                        size - secondDesiredSize - _splitWidth - _splitStart);
                    break;
                }
            }

            if (Vertical)
            {
                first.Arrange(new UIBox2(0, 0, finalSize.X, _splitStart));
                second.Arrange(new UIBox2(0, _splitStart + _splitWidth, finalSize.X, finalSize.Y));
            }
            else
            {
                first.Arrange(new UIBox2(0, 0, _splitStart, finalSize.Y));
                second.Arrange(new UIBox2(_splitStart + _splitWidth, 0, finalSize.X, finalSize.Y));
            }

            return finalSize;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (ChildCount != 2)
            {
                return Vector2.Zero;
            }

            var first = GetChild(0);
            var second = GetChild(1);

            if (State == SplitState.Manual)
            {
                var size = availableSize;
                if (Vertical)
                    size.Y = _splitStart;
                else
                    size.X = _splitStart;

                size = Vector2.ComponentMin(availableSize, size);
                first.Measure(size);

                size = availableSize;
                if (Vertical)
                    size.Y = availableSize.Y - _splitStart - _splitWidth;
                else
                    size.X = availableSize.X - _splitStart - _splitWidth;

                size = Vector2.ComponentMax(size, Vector2.Zero);
                second.Measure(size);
            }
            else
            {
                if (Vertical)
                    availableSize.Y = MathF.Max(0, availableSize.Y - _splitWidth);
                else
                    availableSize.X = MathF.Max(0, availableSize.X - _splitWidth);
                first.Measure(availableSize);

                if (Vertical)
                    availableSize.Y = MathF.Max(0, availableSize.Y - first.DesiredSize.Y);
                else
                    availableSize.X = MathF.Max(0, availableSize.X - first.DesiredSize.X);
                second.Measure(availableSize);
            }

            if (Vertical)
            {
                var width = MathF.Max(first.DesiredSize.X, second.DesiredPixelSize.X);
                var height = first.DesiredSize.Y + _splitWidth + second.DesiredPixelSize.Y;

                return (width, height);
            }
            else
            {
                var width = first.DesiredSize.X + _splitWidth + second.DesiredPixelSize.X;
                var height = MathF.Max(first.DesiredSize.Y, second.DesiredPixelSize.Y);

                return (width, height);
            }
        }

        /// <summary>
        /// Defines how user-initiated moving of the split should work
        /// </summary>
        public enum SplitResizeMode : sbyte
        {
            /// <summary>
            /// Don't allow user to move the split.
            /// </summary>
            NotResizable = -1,

            /// <summary>
            /// User can resize the split but can't shrink either child
            /// beyond its minimum size.
            ///
            /// This ensures that no child ends up with its content outside the
            /// edges of its container.
            /// </summary>
            RespectChildrenMinSize = 0,
        }

        /// <summary>
        /// Defines how the split position should be determined
        /// </summary>
        public enum SplitState : byte
        {
            /// <summary>
            /// Automatically adjust the split based on the width of the children
            /// </summary>
            Auto = 0,

            /// <summary>
            /// Manually adjust the split by dragging it
            /// </summary>
            Manual = 1
        }

        public enum SplitOrientation : byte
        {
            Horizontal,
            Vertical
        }
        
        /// <summary>
        /// Specifies horizontal alignment modes.
        /// </summary>
        /// <seealso cref="Control.HorizontalAlignment"/>
        public enum SplitStretchDirection
        {
            /// <summary>
            /// The control should stretch the the control on the bottom or the right.
            /// </summary>
            BottomRight,
            
            /// <summary>
            /// The control should stretch the the control on the top or the left.
            /// </summary>
            TopLeft,
        }
    }
}
