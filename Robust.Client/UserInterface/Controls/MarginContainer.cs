using Robust.Client.Utility;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap("MarginContainer")]
    public class MarginContainer : Container
    {
        public MarginContainer()
        {
        }

        public MarginContainer(string name) : base(name)
        {
        }

        public int? MarginBottomOverride { get; set; }
        public int? MarginTopOverride { get; set; }
        public int? MarginRightOverride { get; set; }
        public int? MarginLeftOverride { get; set; }

        protected override void SortChildren()
        {
            var top = MarginTopOverride ?? 0;
            var bottom = MarginBottomOverride ?? 0;
            var left = MarginLeftOverride ?? 0;
            var right = MarginRightOverride ?? 0;

            var box = new UIBox2(left, top, Width - right - left, Height - bottom - top);

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

            return (childMinSize.X + left + right, childMinSize.Y + top + bottom);
        }

        private protected override void SetGodotProperty(string property, object value, GodotAssetScene context)
        {
            base.SetGodotProperty(property, value, context);

            switch (property)
            {
                case "custom_constants/margin_right":
                    MarginRightOverride = (int)(long)value;
                    break;
                case "custom_constants/margin_left":
                    MarginLeftOverride = (int)(long)value;
                    break;
                case "custom_constants/margin_bottom":
                    MarginBottomOverride = (int)(long)value;
                    break;
                case "custom_constants/margin_top":
                    MarginTopOverride = (int)(long)value;
                    break;
            }
        }
    }
}
