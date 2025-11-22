using System;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics;

/// <summary>
/// A <see cref="StyleBox"/> that just renders two other style boxes on top of each other.
/// </summary>
internal sealed class StackedStyleBox : StyleBox
{
    private readonly StyleBox _lower;
    private readonly StyleBox _upper;

    public StackedStyleBox(StyleBox lower, StyleBox upper)
    {
        _lower = lower;
        _upper = upper;
    }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        _lower.Draw(handle, box, uiScale);
        _upper.Draw(handle, box, uiScale);
    }

    protected override float GetDefaultContentMargin(Margin margin)
    {
        return Math.Max(_lower.GetContentMargin(margin), _upper.GetContentMargin(margin));
    }
}
