using System;
using System.Numerics;
using Robust.Shared.Maths;
using System.Linq;
using Robust.Client.Graphics;

namespace Robust.Client.UserInterface.Controls;

[Virtual]
public class BoxButton : ContainerButton
{
    public const string StylePropertySeparation = BoxContainer.StylePropertySeparation;

    public BoxContainer.LayoutOrientation Orientation
    {
        get;
        set
        {
            field = value;
            InvalidateMeasure();
        }
    }

    public BoxContainer.AlignMode Align
    {
        get;
        set
        {
            field = value;
            InvalidateArrange();
        }
    }

    public int? SeparationOverride
    {
        get;
        set
        {
            field = value;
            InvalidateMeasure();
        }
    }

    private int Separation => SeparationOverride ?? StylePropertyDefault(StylePropertySeparation, 0);

    private StyleBox StyleBox => StyleBoxOverride ??
                                 StylePropertyDefault(StylePropertyStyleBox,
                                     UserInterfaceManager.ThemeDefaults.ButtonStyle);


    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var boxSize = StyleBox.MinimumSize;
        var childBox = Vector2.Max(availableSize - boxSize, Vector2.Zero);

        var contents = Orientation == BoxContainer.LayoutOrientation.Vertical
            ? MeasureItems<VerticalAxis>(childBox)
            : MeasureItems<HorizontalAxis>(childBox);

        return contents + boxSize;
    }

    private Vector2 MeasureItems<TAxis>(Vector2 availableSize) where TAxis : IAxisImplementation
    {
        availableSize = TAxis.SizeToAxis(availableSize);

        // Account for separation.
        var separation = Separation * (Children.Count(c => c.Visible) - 1);
        var desiredSize = new Vector2(separation, 0);
        availableSize.X = Math.Max(0, availableSize.X) - separation;

        // First, we measure non-stretching children.
        foreach (var child in Children)
        {
            if (!child.Visible)
                continue;

            child.Measure(TAxis.SizeFromAxis(availableSize));
            var childDesired = TAxis.SizeToAxis(child.DesiredSize);

            desiredSize.X += childDesired.X;
            desiredSize.Y = Math.Max(desiredSize.Y, childDesired.Y);

            availableSize.X = Math.Max(0, availableSize.X - childDesired.X);
        }

        return TAxis.SizeFromAxis(desiredSize);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var box = UIBox2.FromDimensions(Vector2.Zero, finalSize);
        var contentBox = StyleBox.GetContentBox(box, 1);
        var separation = Separation;

        if (Orientation == BoxContainer.LayoutOrientation.Vertical)
        {
            BoxContainer.LayOutItems<VerticalAxis>(
                contentBox.TopLeft,
                contentBox.Size,
                Align,
                Children,
                0,
                ChildCount,
                separation);
        }
        else
        {
            BoxContainer.LayOutItems<HorizontalAxis>(
                contentBox.TopLeft,
                contentBox.Size,
                Align,
                Children,
                0,
                ChildCount,
                separation);
        }

        return finalSize;
    }
}
