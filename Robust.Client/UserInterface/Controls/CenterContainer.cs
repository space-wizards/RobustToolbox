using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Container type that centers its children inside itself.
    /// </summary>
    public class CenterContainer : Container
    {
        protected override void LayoutUpdateOverride()
        {
            foreach (var child in Children)
            {
                var childSize = child.CombinedMinimumSize;
                var childPos = (Size - childSize) / 2;

                FitChildInBox(child, UIBox2.FromDimensions(childPos, childSize));
            }
        }
    }
}
