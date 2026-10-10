using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.CustomControls;

/// <summary>
/// A fixed-height virtual list. The owner is responsible for materializing children for the current <see cref="ItemOffset"/>.
/// </summary>
public sealed class VirtualListContainer : Container
{
    // Quick and dirty container to do virtualization of the list.
    // Basically, get total item count and offset to put the current buttons at.
    // Get a constant minimum height and move the buttons in the list up to match the scrollbar.
    private int _totalItemCount;
    private int _itemOffset;
    private float? _itemHeight;

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

    /// <summary>
    ///     The fixed height of every virtual item, excluding <see cref="Separation"/>. If not set, the first
    ///     materialized child determines the height.
    /// </summary>
    public float? ItemHeight
    {
        get => _itemHeight;
        set
        {
            if (value is <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(value));

            if (_itemHeight == value)
                return;

            _itemHeight = value;
            InvalidateMeasure();
        }
    }

    public const float DefaultSeparation = 2;

    public float Separation { get; set; } = DefaultSeparation;

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        if (ChildCount == 0)
        {
            return Vector2.Zero;
        }

        var first = GetChild(0);

        first.Measure(availableSize);
        var (minX, desiredHeight) = first.DesiredSize;
        var minY = ItemHeight ?? desiredHeight;

        return new Vector2(minX, minY * TotalItemCount + (TotalItemCount - 1) * Separation);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        if (ChildCount == 0)
        {
            return Vector2.Zero;
        }

        var first = GetChild(0);

        var height = ItemHeight ?? first.DesiredSize.Y;
        var offset = ItemOffset * (height + Separation);

        foreach (var child in Children)
        {
            child.Arrange(UIBox2.FromDimensions(0, offset, finalSize.X, height));
            offset += Separation + height;
        }

        return finalSize;
    }
}
