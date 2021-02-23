using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Container type that centers its children inside itself.
    /// </summary>
    public class CenterContainer : Container
    {
        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var max = Vector2.Zero;
            foreach (var child in Children)
            {
                var childSize = child.DesiredSize;
                var childPos = (finalSize - childSize) / 2;

                child.Arrange(UIBox2.FromDimensions(childPos, childSize));

                max = Vector2.ComponentMax(max, childSize);
            }
            return max;
        }
    }
}
