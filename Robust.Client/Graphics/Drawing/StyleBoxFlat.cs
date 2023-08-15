using System;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    public sealed class StyleBoxFlat : StyleBox
    {
        public Color BackgroundColor { get; set; }
        public Color BorderColor { get; set; }

        /// <summary>
        /// Thickness of the border, in virtual pixels.
        /// </summary>
        public Thickness BorderThickness { get; set; }

        protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
        {
            var thickness = BorderThickness.Scale(uiScale);
            var (btl, btt, btr, btb) = thickness;
            if (btl > 0)
                handle.DrawRect(new UIBox2(box.Left, box.Top, box.Left + btl, box.Bottom), BorderColor);

            if (btt > 0)
                handle.DrawRect(new UIBox2(box.Left, box.Top, box.Right, box.Top + btt), BorderColor);

            if (btr > 0)
                handle.DrawRect(new UIBox2(box.Right - btr, box.Top, box.Right, box.Bottom), BorderColor);

            if (btb > 0)
                handle.DrawRect(new UIBox2(box.Left, box.Bottom - btb, box.Right, box.Bottom), BorderColor);

            handle.DrawRect(thickness.Deflate(box), BackgroundColor);
        }

        public StyleBoxFlat()
        {
        }

        public StyleBoxFlat(Color backgroundColor)
        {
            BackgroundColor = backgroundColor;
        }

        public StyleBoxFlat(StyleBoxFlat other)
            : base(other)
        {
            BackgroundColor = other.BackgroundColor;
            BorderColor = other.BorderColor;
            BorderThickness = other.BorderThickness;
        }

        protected override float GetDefaultContentMargin(Margin margin)
        {
            return margin switch
            {
                Margin.Top => BorderThickness.Top,
                Margin.Bottom => BorderThickness.Bottom,
                Margin.Right => BorderThickness.Right,
                Margin.Left => BorderThickness.Left,
                _ => throw new ArgumentOutOfRangeException(nameof(margin), margin, null)
            };
        }
    }
}
