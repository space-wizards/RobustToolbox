using System;
using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A container that lays out its children sequentially.
    ///     Use <see cref="VBoxContainer"/> or <see cref="HBoxContainer"/> for an implementation.
    /// </summary>
    public abstract class BoxContainer : Container
    {
        public const string StylePropertySeparation = "separation";

        private const int DefaultSeparation = 0;
        private protected abstract bool Vertical { get; }

        /// <summary>
        ///     Specifies "where" the controls should be laid out.
        /// </summary>
        public AlignMode Align { get; set; }

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

        protected override void LayoutUpdateOverride()
        {
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
                var (minX, minY) = child.CombinedPixelMinimumSize;
                int minSize;
                bool stretch;

                if (Vertical)
                {
                    minSize = minY;
                    stretch = (child.SizeFlagsVertical & SizeFlags.Expand) == SizeFlags.Expand;
                }
                else
                {
                    minSize = minX;
                    stretch = (child.SizeFlagsHorizontal & SizeFlags.Expand) == SizeFlags.Expand;
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

            var stretchMax = Vertical ? PixelHeight : PixelWidth;

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
                    targetBox = new UIBox2i(0, offset, PixelWidth, offset+size);
                }
                else
                {
                    targetBox = new UIBox2i(offset, 0, offset+size, PixelHeight);
                }

                FitChildInPixelBox(control, targetBox);

                offset += size;
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            var separation = ActualSeparation;

            var minWidth = 0f;
            var minHeight = 0f;
            var first = true;

            foreach (var child in Children)
            {
                if (!child.Visible)
                {
                    continue;
                }

                var (childWidth, childHeight) = child.CombinedMinimumSize;
                if (Vertical)
                {
                    minHeight += childHeight;
                    if (!first)
                    {
                        minHeight += separation;
                    }

                    first = false;

                    minWidth = MathF.Max(minWidth, childWidth);
                }
                else
                {
                    minWidth += childWidth;
                    if (!first)
                    {
                        minWidth += separation;
                    }

                    first = false;

                    minHeight = MathF.Max(minHeight, childHeight);
                }
            }

            return new Vector2(minWidth, minHeight);
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
    }
}
