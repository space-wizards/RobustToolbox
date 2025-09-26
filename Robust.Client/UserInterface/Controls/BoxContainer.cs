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
            if (Orientation == LayoutOrientation.Vertical)
            {
                return MeasureItems<VerticalAxis>(availableSize);
            }
            else
            {
                return MeasureItems<HorizontalAxis>(availableSize);
            }
        }

        private Vector2 MeasureItems<TAxis>(Vector2 availableSize) where TAxis : IAxisImplementation
        {
            availableSize = TAxis.SizeToAxis(availableSize);

            // Account for separation.
            var separation = ActualSeparation * (Children.Where(c => c.Visible).Count() - 1);
            var desiredSize = new Vector2(separation, 0);
            availableSize.X = Math.Max(0, availableSize.X) - separation;

            // First, we measure non-stretching children.
            foreach (var child in Children)
            {
                if (!child.Visible)
                    continue;

                child.Measure(TAxis.SizeFromAxis(availableSize));
                var childDesired = TAxis.SizeToAxis(child.DesiredSize);

                desiredSize.X += childDesired.X;
                desiredSize.Y = Math.Max(desiredSize.Y, childDesired.Y);

                availableSize.X = Math.Max(0, availableSize.X - childDesired.X);
            }

            return TAxis.SizeFromAxis(desiredSize);
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var separation = ActualSeparation;

            if (Orientation == LayoutOrientation.Vertical)
            {
                LayOutItems<VerticalAxis>(default, finalSize, Align, Children, 0, ChildCount, separation);
            }
            else
            {
                LayOutItems<HorizontalAxis>(default, finalSize, Align, Children, 0, ChildCount, separation);
            }

            return finalSize;
        }

        internal static void LayOutItems<TAxis>(
            Vector2 baseOffset,
            Vector2 finalSize,
            AlignMode align,
            OrderedChildCollection children,
            int start,
            int end,
            float separation,
            Vector2? fixedSize = null)
            where TAxis : IAxisImplementation
        {
            var realFinalSize = finalSize;
            finalSize = TAxis.SizeToAxis(finalSize);
            fixedSize = fixedSize == null ? null : TAxis.SizeToAxis(fixedSize.Value);

            var visibleChildCount = 0;
            for (var i = start; i < end; i++)
            {
                if (children[i].Visible)
                    visibleChildCount += 1;
            }

            var stretchAvail = finalSize.X;
            stretchAvail -= separation * (visibleChildCount - 1);
            stretchAvail = Math.Max(0, stretchAvail);

            // Step one: figure out the sizes of all our children and whether they want to stretch.
            var sizeList = new List<(Control control, float size, bool stretch)>(visibleChildCount);
            var totalStretchRatio = 0f;
            for (var i = start; i < end; i++)
            {
                var child = children[i];
                if (!child.Visible)
                    continue;

                bool stretch = TAxis.GetMainExpandFlag(child);
                if (!stretch)
                {
                    var measuredSize = fixedSize ?? TAxis.SizeToAxis(child.DesiredSize);
                    var size = measuredSize.X;
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
                        var measuredSize = fixedSize ?? TAxis.SizeToAxis(control.DesiredSize);
                        var desired = measuredSize.X;
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
                switch (align)
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

                var targetBox = TAxis.BoxFromAxis(new UIBox2(offset, 0, offset + size, finalSize.Y), realFinalSize);

                targetBox = targetBox.Translated(baseOffset);

                control.Arrange(targetBox);

                offset += size;
            }
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
