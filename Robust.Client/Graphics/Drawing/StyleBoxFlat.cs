using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    public class StyleBoxFlat : StyleBox
    {
        public Color BackgroundColor { get; set; }

        protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box)
        {
            handle.DrawRect(box, BackgroundColor);
        }

        public StyleBoxFlat()
        {

        }

        public StyleBoxFlat(StyleBoxFlat other)
            : base(other)
        {
            BackgroundColor = other.BackgroundColor;
        }
    }
}
