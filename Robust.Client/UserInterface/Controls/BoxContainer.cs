using System;
using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A container that lays out its children sequentially.
    /// </summary>
    [Virtual]
    public class BoxContainer : Container
    {
        private LayoutOrientation _orientation;
        public const string StylePropertySeparation = "separation";

        private const int DefaultSeparation = 0;

        /// <summary>
        /// Specifies the alignment of the controls <b>along the orientation axis.</b>
        /// </summary>
        /// <remarks>
        /// This is along the orientation axis, not cross to it.
        /// This means that if your orientation is vertical and you set this to center,
        /// your controls will be laid out in the vertical <i>center</i> of the box control instead of the top.
        /// </remarks>
        public AlignMode Align { get; set; }

        private bool Vertical => Orientation == LayoutOrientation.Vertical;

        public LayoutOrientation Orientation
        {
            get => _orientation;
            set
            {
                _orientation = value;
                InvalidateMeasure();
            }
        }

        private int ActualSeparation
        {
            get
            {
                if (TryGetStyleProperty(StylePropertySeparation, out int separation))
                {
                    return separation;
                }

                return SeparationOverride ?? DefaultSeparation;
            }
        }

        public int? SeparationOverride { get; set; }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var separation = ActualSeparation;

            var minSize = Vector2.Zero;
            var first = true;

            foreach (var child in Children)
            {
                if (!child.Visible)
                {
                    continue;
                }

                child.Measure(availableSize);

                var childSize = child.DesiredSize;
                if (Vertical)
                {
                    var taken = childSize.Y;
                    if (!first)
                    {
                        taken += separation;
                    }

                    minSize.Y += taken;
                    availableSize.Y = Math.Max(0, availableSize.Y - taken);

                    first = false;
                    minSize.X = Math.Max(minSize.X, childSize.X);
                }
                else
                {
                    var taken = childSize.X;
                    if (!first)
                    {
                        taken += separation;
                    }

                    minSize.X += taken;
                    availableSize.X = Math.Max(0, availableSize.X - taken);

                    first = false;

                    minSize.Y = Math.Max(minSize.Y, childSize.Y);
                }
            }

            return minSize;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var finalPixel = (Vector2i) (finalSize * UIScale);
            var separation = (int) (ActualSeparation * UIScale);

            // Step one: figure out the sizes of all our children and whether they want to stretch.
            var sizeList = new List<(Control control, int minSize, int finalSize, bool stretch)>(ChildCount);
            var totalStretchRatio = 0f;
            // Amount of space not available for stretching.
            var stretchMin = 0;

            foreach (var child in Children)
            {
                if (!child.Visible)
                {
                    continue;
                }

                var (minX, minY) = child.DesiredPixelSize;
                int minSize;
                bool stretch;

                if (Vertical)
                {
                    minSize = minY;
                    stretch = child.VerticalExpand;
                }
                else
                {
                    minSize = minX;
                    stretch = child.HorizontalExpand;
                }

                if (!stretch)
                {
                    stretchMin += minSize;
                }
                else
                {
                    totalStretchRatio += child.SizeFlagsStretchRatio;
                }

                sizeList.Add((child, minSize, minSize, stretch));
            }

            var stretchMax = Vertical ? finalPixel.Y : finalPixel.X;

            stretchMax -= separation * (ChildCount - 1);
            // This is the amount of space allocated for stretchable children.
            var stretchAvail = Math.Max(0, stretchMax - stretchMin);

            // Step two: figure out which that want to stretch need to suck it,
            // because due to their stretch ratio they would be smaller than minSize.
            // Treat those as non-stretching.
            for (var i = 0; i < sizeList.Count; i++)
            {
                var (control, minSize, _, stretch) = sizeList[i];
                if (!stretch)
                {
                    continue;
                }

                var share = (int) (stretchAvail * (control.SizeFlagsStretchRatio / totalStretchRatio));
                if (share < minSize)
                {
                    sizeList[i] = (control, minSize, minSize, false);
                    stretchAvail -= minSize;
                    totalStretchRatio -= control.SizeFlagsStretchRatio;
                }
            }

            // Step three: allocate space for all the stretchable children.
            var stretchingAtAll = false;
            for (var i = 0; i < sizeList.Count; i++)
            {
                var (control, minSize, _, stretch) = sizeList[i];
                if (!stretch)
                {
                    continue;
                }

                stretchingAtAll = true;

                var share = (int) (stretchAvail * (control.SizeFlagsStretchRatio / totalStretchRatio));
                sizeList[i] = (control, minSize, share, false);
            }

            // Step four: actually lay them out one by one.
            var offset = 0;
            if (!stretchingAtAll)
            {
                switch (Align)
                {
                    case AlignMode.Begin:
                        break;
                    case AlignMode.Center:
                        offset = stretchAvail / 2;
                        break;
                    case AlignMode.End:
                        offset = stretchAvail;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var first = true;
            foreach (var (control, _, size, _) in sizeList)
            {
                if (!first)
                {
                    offset += separation;
                }

                first = false;

                UIBox2i targetBox;
                if (Vertical)
                {
                    targetBox = new UIBox2i(0, offset, finalPixel.X, offset + size);
                }
                else
                {
                    targetBox = new UIBox2i(offset, 0, offset + size, finalPixel.Y);
                }

                control.ArrangePixel(targetBox);

                offset += size;
            }

            return finalSize;
        }

        public enum AlignMode : byte
        {
            /// <summary>
            ///     Controls are laid out from the begin of the box container.
            /// </summary>
            Begin = 0,

            /// <summary>
            ///     Controls are laid out from the center of the box container.
            /// </summary>
            Center = 1,

            /// <summary>
            ///     Controls are laid out from the end of the box container.
            /// </summary>
            End = 2
        }

        /// <summary>
        /// Orientation for a box container.
        /// </summary>
        public enum LayoutOrientation : byte
        {
            /// <summary>
            /// Controls are laid out horizontally, left to right.
            /// </summary>
            Horizontal,

            /// <summary>
            /// Controls are laid out vertically, top to bottom.
            /// </summary>
            Vertical
        }
    }
}
