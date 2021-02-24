using System;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [Obsolete("Set Margin directly")]
    public class MarginContainer : Container
    {
        public int? MarginBottomOverride { get; set; }
        public int? MarginTopOverride { get; set; }
        public int? MarginRightOverride { get; set; }
        public int? MarginLeftOverride { get; set; }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var margin = GetMargin();
            var availWithoutMargin = margin.Deflate(availableSize);

            var max = Vector2.Zero;
            foreach (var child in Children)
            {
                child.Measure(availWithoutMargin);
                max = Vector2.ComponentMax(max, child.DesiredSize);
            }

            return margin.Inflate(max);
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var box = GetMargin().Deflate(UIBox2.FromDimensions(Vector2.Zero, finalSize));

            foreach (var child in Children)
            {
                child.Arrange(box);
            }

            return finalSize;
        }

        private Thickness GetMargin()
        {
            var top = MarginTopOverride ?? 0;
            var bottom = MarginBottomOverride ?? 0;
            var left = MarginLeftOverride ?? 0;
            var right = MarginRightOverride ?? 0;
            var margin = new Thickness(left, top, right, bottom);
            return margin;
        }
    }
}
