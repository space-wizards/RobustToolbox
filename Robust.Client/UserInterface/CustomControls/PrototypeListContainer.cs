using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.CustomControls;

public sealed class PrototypeListContainer : Container
{
    // Quick and dirty container to do virtualization of the list.
    // Basically, get total item count and offset to put the current buttons at.
    // Get a constant minimum height and move the buttons in the list up to match the scrollbar.
    private int _totalItemCount;
    private int _itemOffset;

    public int TotalItemCount
    {
        get => _totalItemCount;
        set
        {
            _totalItemCount = value;
            InvalidateMeasure();
        }
    }

    public int ItemOffset
    {
        get => _itemOffset;
        set
        {
            _itemOffset = value;
            InvalidateMeasure();
        }
    }

    public const float Separation = 2;

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        if (ChildCount == 0)
        {
            return Vector2.Zero;
        }

        var first = GetChild(0);

        first.Measure(availableSize);
        var (minX, minY) = first.DesiredSize;

        return new Vector2(minX, minY * TotalItemCount + (TotalItemCount - 1) * Separation);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        if (ChildCount == 0)
        {
            return Vector2.Zero;
        }

        var first = GetChild(0);

        var height = first.DesiredSize.Y;
        var offset = ItemOffset * height + (ItemOffset - 1) * Separation;

        foreach (var child in Children)
        {
            child.Arrange(UIBox2.FromDimensions(0, offset, finalSize.X, height));
            offset += Separation + height;
        }

        return finalSize;
    }
}
