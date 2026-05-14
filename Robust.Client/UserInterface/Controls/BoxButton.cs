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
            ? BoxContainer.MeasureItems<VerticalAxis>(childBox, Children, Separation)
            : BoxContainer.MeasureItems<HorizontalAxis>(childBox, Children, Separation);

        return contents + boxSize;
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
