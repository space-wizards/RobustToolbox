using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.CustomControls;

internal sealed class DoNotMeasure : Control
{
    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        return Vector2.Zero;
    }
}
