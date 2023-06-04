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
            var remainingSize = availableSize;

            // Account for separation.
            var separation = ActualSeparation * (ChildCount - 1);
            var desiredSize = Vector2.Zero;
            if (Vertical)
            {
                desiredSize.Y += separation;
                remainingSize.Y -= separation;
            }
            else
            {
                desiredSize.X += separation;
                remainingSize.X -= separation;
            }

            // First, we measure non-stretching children.
            var stretching = new List<Control>();
            float totalStretchRatio = 0;
            foreach (var child in Children)
            {
                if (!child.Visible)
                    continue;

                var stretch = Vertical ? child.VerticalExpand : child.HorizontalExpand;
                if (stretch)
                {
                    totalStretchRatio += child.SizeFlagsStretchRatio;
                    stretching.Add(child);
                    continue;
                }

                child.Measure(remainingSize);

                if (Vertical)
                {
                    desiredSize.Y += child.DesiredSize.Y;
                    desiredSize.X = Math.Max(desiredSize.X, child.DesiredSize.X);
                    remainingSize.Y = Math.Max(0, remainingSize.Y - child.DesiredSize.Y);
                }
                else
                {
                    desiredSize.X += child.DesiredSize.X;
                    desiredSize.Y = Math.Max(desiredSize.Y, child.DesiredSize.Y);
                    remainingSize.X = Math.Max(0, remainingSize.X - child.DesiredSize.X);
                }
            }

            // Measure stretching children
            foreach (var child in stretching)
            {
                var size = remainingSize;
                if (Vertical)
                {
                    size.Y *= child.SizeFlagsStretchRatio / totalStretchRatio;
                    child.Measure(size);
                    desiredSize.Y += child.DesiredSize.Y;
                    desiredSize.X = Math.Max(desiredSize.X, child.DesiredSize.X);
                }
                else
                {
                    size.X *= child.SizeFlagsStretchRatio / totalStretchRatio;
                    child.Measure(size);
                    desiredSize.X += child.DesiredSize.X;
                    desiredSize.Y = Math.Max(desiredSize.Y, child.DesiredSize.Y);
                }
            }

            return desiredSize;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var finalPixel = (Vector2i) (finalSize * UIScale);
            var separation = (int) (ActualSeparation * UIScale);

            var stretchAvail = Vertical ? finalPixel.Y : finalPixel.X;
            stretchAvail -= separation * (ChildCount - 1);

            // Step one: figure out the sizes of all our children and whether they want to stretch.
            var sizeList = new List<(Control control, int size, bool stretch)>(ChildCount);
            var totalStretchRatio = 0f;

            foreach (var child in Children)
            {
                if (!child.Visible)
                    continue;

                bool stretch = Vertical ? child.VerticalExpand : child.HorizontalExpand;
                if (!stretch)
                {
                    var size = Vertical ? child.DesiredPixelSize.Y : child.DesiredPixelSize.X;
                    size = Math.Clamp(size, 0, stretchAvail);
                    stretchAvail -= size;
                    sizeList.Add((child, size, false));
                }
                else
                {
                    totalStretchRatio += child.SizeFlagsStretchRatio;
                    sizeList.Add((child, 0, true));
                }
            }
            stretchAvail = Math.Max(0, stretchAvail);


            // Possible optional step / behaviour:
            // Maybe limit the available stretch space that controls can take up if they have a max size?


            // Step two: allocate space for all the stretchable children.
            var offset = 0;
            var anyStretch = totalStretchRatio > 0;
            if (anyStretch)
            {
                for (var i = 0; i < sizeList.Count; i++)
                {
                    var (control, _, stretch) = sizeList[i];
                    if (!stretch)
                        continue;

                    var share = (int) (stretchAvail * control.SizeFlagsStretchRatio / totalStretchRatio);
                    sizeList[i] = (control, share, false);
                }
            }
            else
            {
                // No stretching children -> offset the children based on the alignment.
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

            // Step three: actually lay them out one by one.
            var first = true;
            foreach (var (control, size, _) in sizeList)
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
