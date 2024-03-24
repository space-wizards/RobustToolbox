using System.Numerics;
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

            var style = GetStyleBox();
            style?.Draw(handle, PixelSizeBox, UIScale);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var styleSize = GetStyleBox()?.MinimumSize ?? Vector2.Zero;
            var measureSize = Vector2.Max(availableSize - styleSize, Vector2.Zero);
            var childSize = Vector2.Zero;
            foreach (var child in Children)
            {
                child.Measure(measureSize);
                childSize = Vector2.Max(childSize, child.DesiredSize);
            }

            return styleSize + childSize;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var ourSize = UIBox2.FromDimensions(Vector2.Zero, finalSize);
            var contentBox = GetStyleBox()?.GetContentBox(ourSize, 1) ?? ourSize;

            foreach (var child in Children)
            {
                child.Arrange(contentBox);
            }

            return finalSize;
        }

        [System.Diagnostics.Contracts.Pure]
        protected StyleBox? GetStyleBox()
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
