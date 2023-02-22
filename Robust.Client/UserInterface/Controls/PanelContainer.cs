using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
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

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var styleSize = (_getStyleBox()?.MinimumSize ?? Vector2.Zero) / UIScale;
            var measureSize = Vector2.ComponentMax(availableSize - styleSize, Vector2.Zero);
            var childSize = Vector2.Zero;
            foreach (var child in Children)
            {
                child.Measure(measureSize);
                childSize = Vector2.ComponentMax(childSize, child.DesiredSize);
            }

            return styleSize + childSize;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var style = _getStyleBox();

            UIBox2 contentBox;
            if (style == null)
            {
                contentBox = UIBox2.FromDimensions(Vector2.Zero, finalSize);
            }
            else
            {
                var scale = UIScale;
                var pixelBox = UIBox2.FromDimensions(Vector2.Zero, finalSize * scale);
                var contentPixelBox = style.GetContentBox(pixelBox);
                contentBox = new UIBox2(contentPixelBox.TopLeft/scale, contentPixelBox.BottomRight/scale);
            }

            foreach (var child in Children)
            {
                child.Arrange(contentBox);
            }

            return finalSize;
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
