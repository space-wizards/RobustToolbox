using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
            // Account for separation.
            var separation = ActualSeparation * (Children.Where(c => c.Visible).Count() - 1);
            var desiredSize = Vector2.Zero;
            if (Vertical)
            {
                desiredSize.Y += separation;
                availableSize.Y = Math.Max(0, availableSize.Y - separation);
            }
            else
            {
                desiredSize.X += separation;
                availableSize.X = Math.Max(0, availableSize.X - separation);
            }

            // First, we measure non-stretching children.
            foreach (var child in Children)
            {
                if (!child.Visible)
                    continue;

                child.Measure(availableSize);

                if (Vertical)
                {
                    desiredSize.Y += child.DesiredSize.Y;
                    desiredSize.X = Math.Max(desiredSize.X, child.DesiredSize.X);
                    availableSize.Y = Math.Max(0, availableSize.Y - child.DesiredSize.Y);
                }
                else
                {
                    desiredSize.X += child.DesiredSize.X;
                    desiredSize.Y = Math.Max(desiredSize.Y, child.DesiredSize.Y);
                    availableSize.X = Math.Max(0, availableSize.X - child.DesiredSize.X);
                }
            }

            return desiredSize;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var separation = ActualSeparation;
            var visibleChildCount = Children.Where(c => c.Visible).Count();

            var stretchAvail = Vertical ? finalSize.Y : finalSize.X;
            stretchAvail -= separation * (visibleChildCount - 1);
            stretchAvail = Math.Max(0, stretchAvail);

            // Step one: figure out the sizes of all our children and whether they want to stretch.
            var sizeList = new List<(Control control, float size, bool stretch)>(visibleChildCount);
            var totalStretchRatio = 0f;
            foreach (var child in Children)
            {
                if (!child.Visible)
                    continue;

                bool stretch = Vertical ? child.VerticalExpand : child.HorizontalExpand;
                if (!stretch)
                {
                    var size = Vertical ? child.DesiredSize.Y : child.DesiredSize.X;
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

            // Step two: allocate space for all the stretchable children.
            float offset = 0;
            var anyStretch = totalStretchRatio > 0;
            if (anyStretch)
            {
                // We will treat stretching children that fail to reach their desired size as non-stretching.
                // This then requires all stretching children to be re-stretched
                bool stretchAvailChanged = true;
                while (stretchAvailChanged)
                {
                    stretchAvailChanged = false;
                    for (var i = 0; i < sizeList.Count; i++)
                    {
                        var (control, _, stretch) = sizeList[i];
                        if (!stretch)
                            continue;

                        var share = stretchAvail * control.SizeFlagsStretchRatio / totalStretchRatio;
                        var desired = Vertical ? control.DesiredSize.Y : control.DesiredSize.X;
                        if (share >= desired)
                        {
                            sizeList[i] = (control, share, true);
                            continue;
                        }

                        // Insufficient space -> treat as non-stretching.
                        sizeList[i] = (control, Math.Min(stretchAvail, desired), false);
                        stretchAvail = Math.Max(0, stretchAvail - desired);
                        totalStretchRatio -= control.SizeFlagsStretchRatio;
                        stretchAvailChanged = true;
                    }
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

                UIBox2 targetBox;
                if (Vertical)
                {
                    targetBox = new UIBox2(0, offset, finalSize.X, offset + size);
                }
                else
                {
                    targetBox = new UIBox2(offset, 0, offset + size, finalSize.Y);
                }

                control.Arrange(targetBox);

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
