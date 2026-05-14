using System.Numerics;
using Robust.Shared.Maths;
using Robust.Client.Graphics;

namespace Robust.Client.UserInterface.Controls;

/// <summary>
/// A button with all the features but lays out children like a BoxContainer.
/// </summary>
/// <remarks>
/// This control does not wrap an internal <see cref="BoxContainer"/>, all children are directly parented to this control.
/// </remarks>
[Virtual]
public class BoxButton : ContainerButton
{
    /// <summary>
    /// Style property for the amount of spacing inserted between visible children.
    /// </summary>
    public const string StylePropertySeparation = BoxContainer.StylePropertySeparation;

    /// <summary>
    /// The direction in which children are laid out.
    /// </summary>
    public BoxContainer.LayoutOrientation Orientation
    {
        get;
        set
        {
            field = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Changes how children are aligned along the orientation axis when there is unused space.
    /// </summary>
    public BoxContainer.AlignMode Align
    {
        get;
        set
        {
            field = value;
            InvalidateArrange();
        }
    }

    /// <summary>
    /// Overrides <see cref="StylePropertySeparation"/>, setting the separation spacing between children.
    /// </summary>
    public int? SeparationOverride
    {
        get;
        set
        {
            field = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Actual spacing between children.
    /// </summary>
    private int Separation => SeparationOverride ?? StylePropertyDefault(StylePropertySeparation, 0);

    /// <summary>
    /// The style box, for both drawing the background and content margins.
    /// </summary>
    private StyleBox StyleBox => StyleBoxOverride ??
                                 StylePropertyDefault(StylePropertyStyleBox,
                                     UserInterfaceManager.ThemeDefaults.ButtonStyle);

    /// <inheritdoc />
    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var boxSize = StyleBox.MinimumSize;
        var childBox = Vector2.Max(availableSize - boxSize, Vector2.Zero);

        var contents = Orientation == BoxContainer.LayoutOrientation.Vertical
            ? BoxContainer.MeasureItems<VerticalAxis>(childBox, Children, Separation)
            : BoxContainer.MeasureItems<HorizontalAxis>(childBox, Children, Separation);

        return contents + boxSize;
    }

    /// <inheritdoc />
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
