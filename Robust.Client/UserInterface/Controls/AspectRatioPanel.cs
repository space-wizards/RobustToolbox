using System.Numerics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls;

// TODO BEFORE MAKING PUBLIC:
// Do we need to constrain the aspect ratio in MeasureOverride() too?
// Doc comments

/// <summary>
/// A simple UI control that ensures its children are laid out with a fixed aspect ratio.
/// </summary>
internal sealed class AspectRatioPanel : Control
{
    public AspectRatio AspectRatio
    {
        get;
        set
        {
            field = value;
            InvalidateArrange();
        }
    } = AspectRatio.One;

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var givenRatio = finalSize.X / finalSize.Y;

        if (!MathHelper.CloseTo(givenRatio, AspectRatio.Ratio))
        {
            if (givenRatio < AspectRatio.Ratio)
            {
                // Too narrow, derive height from width.
                finalSize = finalSize with { Y = finalSize.X / AspectRatio.Ratio };
            }
            else
            {
                // Too wide, derive width from height.
                finalSize = finalSize with { X = finalSize.Y * AspectRatio.Ratio };
            }
        }

        return base.ArrangeOverride(finalSize);
    }
}

internal struct AspectRatio(float ratio)
{
    public static readonly AspectRatio One = new(1);

    public float Ratio = ratio;

    public static AspectRatio FromWidthHeight(float width, float height)
    {
        return new AspectRatio(width / height);
    }
}
