using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.CenterContainer))]
    public class CenterContainer : Container
    {
        public CenterContainer() {}
        public CenterContainer(Godot.CenterContainer control) : base(control) {}

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.CenterContainer();
        }

        protected override void SortChildren()
        {
            foreach (var child in Children)
            {
                var childSize = child.CombinedMinimumSize;
                var childPos = (Size - childSize) / 2;

                FitChildInBox(child, UIBox2.FromDimensions(childPos, childSize));
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot)
            {
                return Vector2.Zero;
            }

            var min = Vector2.Zero;

            foreach (var child in Children)
            {
                min = Vector2.ComponentMax(child.CombinedMinimumSize, min);
            }

            return min;
        }
    }
}
