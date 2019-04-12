using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.MarginContainer))]
    public class MarginContainer : Container
    {
        public MarginContainer()
        {
        }

        public MarginContainer(string name) : base(name)
        {
        }

        internal MarginContainer(Godot.MarginContainer sceneControl) : base(sceneControl)
        {
        }

        private int? _marginBottomOverride;

        public int? MarginBottomOverride
        {
            get => _marginBottomOverride ?? GetConstantOverride("margin_bottom");
            set => SetConstantOverride("margin_bottom", _marginBottomOverride = value);
        }

        private int? _marginTopOverride;

        public int? MarginTopOverride
        {
            get => _marginTopOverride ?? GetConstantOverride("margin_top");
            set => SetConstantOverride("margin_top", _marginTopOverride = value);
        }

        private int? _marginRightOverride;

        public int? MarginRightOverride
        {
            get => _marginRightOverride ?? GetConstantOverride("margin_right");
            set => SetConstantOverride("margin_right", _marginRightOverride = value);
        }

        private int? _marginLeftOverride;

        public int? MarginLeftOverride
        {
            get => _marginLeftOverride ?? GetConstantOverride("margin_left");
            set => SetConstantOverride("margin_left", _marginLeftOverride = value);
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.MarginContainer();
        }

        protected override void SortChildren()
        {
            var top = _marginTopOverride ?? 0;
            var bottom = _marginBottomOverride ?? 0;
            var left = _marginLeftOverride ?? 0;
            var right = _marginRightOverride ?? 0;

            var box = new UIBox2(left, top, Width - right - left, Height - bottom - top);

            foreach (var child in Children)
            {
                FitChildInBox(child, box);
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot)
            {
                return Vector2.Zero;
            }

            var top = _marginTopOverride ?? 0;
            var bottom = _marginBottomOverride ?? 0;
            var left = _marginLeftOverride ?? 0;
            var right = _marginRightOverride ?? 0;

            var childMinSize = Vector2.Zero;

            foreach (var child in Children)
            {
                childMinSize = Vector2.ComponentMax(child.CombinedMinimumSize, childMinSize);
            }

            return (childMinSize.X + left + right, childMinSize.Y + top + bottom);
        }
    }
}
