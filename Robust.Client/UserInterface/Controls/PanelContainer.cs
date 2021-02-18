using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public class PanelContainer : Container
    {
        public const string StylePropertyPanel = "panel";

        public StyleBox? PanelOverride { get; set; }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var style = _getStyleBox();
            style?.Draw(handle, PixelSizeBox);
        }

        protected override void LayoutUpdateOverride()
        {
            var contentBox = _getStyleBox()?.GetContentBox(PixelSizeBox) ?? PixelSizeBox;

            foreach (var child in Children)
            {
                FitChildInPixelBox(child, (UIBox2i) contentBox);
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            var styleSize = _getStyleBox()?.MinimumSize ?? Vector2.Zero;
            var childSize = Vector2.Zero;
            foreach (var child in Children)
            {
                childSize = Vector2.ComponentMax(childSize, child.CombinedMinimumSize);
            }

            return styleSize / UIScale + childSize;
        }

        [System.Diagnostics.Contracts.Pure]
        private StyleBox? _getStyleBox()
        {
            if (PanelOverride != null)
            {
                return PanelOverride;
            }

            TryGetStyleProperty<StyleBox>(StylePropertyPanel, out var box);
            return box;
        }
    }
}
