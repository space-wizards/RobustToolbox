using System;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap("SplitContainer")]
    public abstract class SplitContainer : Container
    {
        // TODO: Implement the grabber.
        private const float Separation = 2;

        private protected abstract bool Vertical { get; }

        protected internal override void SortChildren()
        {
            base.SortChildren();

            if (ChildCount != 2)
            {
                return;
            }

            var first = GetChild(0);
            var second = GetChild(1);

            var firstExpand = Vertical
                ? (first.SizeFlagsVertical & SizeFlags.Expand) != 0
                : (first.SizeFlagsHorizontal & SizeFlags.Expand) != 0;
            var secondExpand = Vertical
                ? (second.SizeFlagsVertical & SizeFlags.Expand) != 0
                : (second.SizeFlagsHorizontal & SizeFlags.Expand) != 0;

            var firstMinSize = Vertical ? first.CombinedMinimumSize.Y : first.CombinedMinimumSize.X;
            var secondMinSize = Vertical ? second.CombinedMinimumSize.Y : second.CombinedMinimumSize.X;

            var size = Vertical ? Height : Width;

            var ratio = first.SizeFlagsStretchRatio / (first.SizeFlagsStretchRatio + second.SizeFlagsStretchRatio);
            float offsetCenter;

            if (firstExpand && secondExpand)
            {
                offsetCenter = size * ratio - Separation / 2;
            }
            else if (firstExpand)
            {
                offsetCenter = size - secondMinSize - Separation;
            }
            else
            {
                offsetCenter = firstMinSize;
            }

            offsetCenter += 0f.Clamp(firstMinSize - offsetCenter, size - secondMinSize - Separation - offsetCenter);

            if (Vertical)
            {
                FitChildInBox(first, new UIBox2(0, 0, Width, offsetCenter));
                FitChildInBox(second, new UIBox2(0, offsetCenter + Separation, Width, Height));
            }
            else
            {
                FitChildInBox(first, new UIBox2(0, 0, offsetCenter, Height));
                FitChildInBox(second, new UIBox2(offsetCenter + Separation, 0, Width, Height));
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (ChildCount != 2)
            {
                return Vector2.Zero;
            }

            var first = GetChild(0);
            var second = GetChild(1);

            var (firstSizeX, firstSizeY) = first.CombinedMinimumSize;
            var (secondSizeX, secondSizeY) = second.CombinedMinimumSize;

            if (Vertical)
            {
                var width = Math.Max(firstSizeX, secondSizeX);
                var height = firstSizeY + Separation + secondSizeY;

                return (width, height);
            }
            else
            {
                var width = firstSizeX + Separation + secondSizeX;
                var height = Math.Max(firstSizeY, secondSizeY);

                return (width, height);
            }
        }
    }
}
