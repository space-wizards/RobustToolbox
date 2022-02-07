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
        public float SplitWidth { get; set; }

        /// <summary>
        /// Virtual pixel offset from the edge beyond which the split cannot be moved.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SplitEdgeSeparation { get; set; }

        private float _splitCenter;
        private SplitState _splitState;
        private bool _dragging;
        private SplitOrientation _orientation;

        private bool Vertical => Orientation == SplitOrientation.Vertical;

        public SplitState State
        {
            get => _splitState;
            set
            {
                _splitState = value;
                InvalidateMeasure();
            }
        }

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

                _splitCenter = ClampSplitCenter(newOffset, Size);
                DefaultCursorShape = Vertical ? CursorShape.VResize : CursorShape.HResize;
                InvalidateArrange();
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
            if (Vertical)
            {
                return Math.Abs(relativePosition.Y - _splitCenter) <= SplitWidth;
            }

            return Math.Abs(relativePosition.X - _splitCenter) <= SplitWidth;
        }

        /// <summary>
        /// Ensures the split center is within all necessary limits
        /// </summary>
        /// <param name="splitCenter">proposed split location</param>
        /// <param name="firstMinSize">min size of the first child, will calculate if null</param>
        /// <param name="secondMinSize">min size of the second child, will calculate if null</param>
        /// <returns></returns>
        private float ClampSplitCenter(float splitCenter, Vector2 controlSize, float? firstMinSize = null, float? secondMinSize = null)
        {
            // min / max x and y extents in relative virtual pixels of where the split can go regardless
            // of anything else.

            var splitMin = SplitWidth + SplitEdgeSeparation;
            var splitMax = Vertical ? controlSize.Y - splitMin : controlSize.X - splitMin;
            splitCenter = MathHelper.Clamp(splitCenter, splitMin, splitMax);

            if (ResizeMode == SplitResizeMode.RespectChildrenMinSize && ChildCount == 2)
            {
                var first = GetChild(0);
                var second = GetChild(1);

                firstMinSize ??= (Vertical ? first.DesiredSize.Y : first.DesiredSize.X);
                secondMinSize ??= (Vertical ? second.DesiredSize.Y : second.DesiredSize.X);
                var size = Vertical ? controlSize.Y : controlSize.X;

                splitCenter = MathHelper.Clamp(splitCenter, firstMinSize.Value,
                    size - (secondMinSize.Value + SplitWidth));
            }

            return splitCenter;
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

            var firstMinSize = Vertical ? first.DesiredSize.Y : first.DesiredSize.X;
            var secondMinSize = Vertical ? second.DesiredSize.Y : second.DesiredSize.X;

            var size = Vertical ? finalSize.Y : finalSize.X;

            var ratio = first.SizeFlagsStretchRatio / (first.SizeFlagsStretchRatio + second.SizeFlagsStretchRatio);

            switch (_splitState)
            {
                case SplitState.Manual:
                    // min sizes of children may have changed, ensure the offset still respects the defined limits
                    _splitCenter = ClampSplitCenter(_splitCenter, finalSize, firstMinSize, secondMinSize);
                    break;
                case SplitState.Auto:
                {
                    if (firstExpand && secondExpand)
                    {
                        _splitCenter = size * ratio - SplitWidth / 2;
                    }
                    else if (firstExpand)
                    {
                        _splitCenter = size - secondMinSize - SplitWidth;
                    }
                    else
                    {
                        _splitCenter = firstMinSize;
                    }

                    _splitCenter += MathHelper.Clamp(0f, firstMinSize - _splitCenter,
                        size - secondMinSize - SplitWidth - _splitCenter);
                    break;
                }
            }

            if (Vertical)
            {
                first.Arrange(new UIBox2(0, 0, finalSize.X, _splitCenter));
                second.Arrange(new UIBox2(0, _splitCenter + SplitWidth, finalSize.X, finalSize.Y));
            }
            else
            {
                first.Arrange(new UIBox2(0, 0, _splitCenter, finalSize.Y));
                second.Arrange(new UIBox2(_splitCenter + SplitWidth, 0, finalSize.X, finalSize.Y));
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

            // TODO: Probably bad implementation with the new WPF layout.
            first.Measure(availableSize);
            second.Measure(availableSize);

            var (firstSizeX, firstSizeY) = first.DesiredSize;
            var (secondSizeX, secondSizeY) = second.DesiredSize;

            if (Vertical)
            {
                var width = MathF.Max(firstSizeX, secondSizeX);
                var height = firstSizeY + SplitWidth + secondSizeY;

                return (width, height);
            }
            else
            {
                var width = firstSizeX + SplitWidth + secondSizeX;
                var height = MathF.Max(firstSizeY, secondSizeY);

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
    }
}
