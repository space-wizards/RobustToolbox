using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public class MarginContainer : Container
    {
        public int? MarginBottomOverride { get; set; }
        public int? MarginTopOverride { get; set; }
        public int? MarginRightOverride { get; set; }
        public int? MarginLeftOverride { get; set; }

        protected override void LayoutUpdateOverride()
        {
            var top = MarginTopOverride ?? 0;
            var bottom = MarginBottomOverride ?? 0;
            var left = MarginLeftOverride ?? 0;
            var right = MarginRightOverride ?? 0;

            var box = UIBox2.FromDimensions(left, top, Width - right - left, Height - bottom - top);

            foreach (var child in Children)
            {
                FitChildInBox(child, box);
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            var top = MarginTopOverride ?? 0;
            var bottom = MarginBottomOverride ?? 0;
            var left = MarginLeftOverride ?? 0;
            var right = MarginRightOverride ?? 0;

            var childMinSize = Vector2.Zero;

            foreach (var child in Children)
            {
                childMinSize = Vector2.ComponentMax(child.CombinedMinimumSize, childMinSize);
            }

            return childMinSize + (left + right, top + bottom);
        }
    }
}
