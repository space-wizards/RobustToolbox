using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    public sealed class StyleBoxEmpty : StyleBox
    {
        protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box)
        {
            // It's empty what more do you want?
        }
    }
}
