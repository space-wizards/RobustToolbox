using System;
using System.Numerics;
using Robust.Shared.Collections;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls;

/// <summary>
/// Lays of children sequentially, "wrapping" them onto another row/column if necessary.
/// </summary>
public sealed class WrapContainer : Container
{
    /// <summary>
    /// Specifies the amount of space between two children, on the main axis.
    /// </summary>
    public const string StylePropertySeparation = "separation";

    /// <summary>
    /// Specifies the amount of space between two children, on the cross axis.
    /// </summary>
    public const string StylePropertyCrossSeparation = "cross-separation";

    // Parameters.
    private Axis _layoutAxis;
    private ItemJustification _justification;
    private bool _equalSize;
    private bool _reverse;
    private int? _separationOverride;
    private int? _crossSeparationOverride;

    // Cached layout data.
    private ValueList<(int endIndex, float cross)> _rowIndices;
    private float _lastMeasureCross;

    /// <summary>
    /// Specifies the amount of space between two children, on the main axis.
    /// </summary>
    /// <remarks>
    /// This property overrides <see cref="StylePropertySeparation"/>, if set.
    /// </remarks>
    public int? SeparationOverride
    {
        get => _separationOverride;
        set
        {
            _separationOverride = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Specifies the amount of space between two children, on the cross axis.
    /// </summary>
    /// <remarks>
    /// This property overrides <see cref="StylePropertyCrossSeparation"/>, if set.
    /// </remarks>
    public int? CrossSeparationOverride
    {
        get => _crossSeparationOverride;
        set
        {
            _crossSeparationOverride = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// The <see cref="Axis"/> along which to lay out children.
    /// </summary>
    public Axis LayoutAxis
    {
        get => _layoutAxis;
        set
        {
            _layoutAxis = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// If true, all children will be laid out with the size of the largest child.
    /// </summary>
    public bool EqualSize
    {
        get => _equalSize;
        set
        {
            _equalSize = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Determines where items will be laid out on an individual row/column.
    /// </summary>
    public ItemJustification Justification
    {
        get => _justification;
        set
        {
            _justification = value;
            InvalidateArrange();
        }
    }

    /// <summary>
    /// If true, reverses the order on which wrapping rows/columns are laid out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On horizontal axis, the first children are on the top row, and new rows are added <i>downwards</i>.
    /// With <see cref="Reverse"/> set to true,
    /// the first children are instead on the bottom row, with new rows growing upwards.
    /// </para>
    /// </remarks>
    public bool Reverse
    {
        get => _reverse;
        set
        {
            _reverse = value;
            InvalidateArrange();
        }
    }

    private int ActualSeparation
    {
        get
        {
            if (TryGetStyleProperty(StylePropertySeparation, out int separation))
            {
                return separation;
            }

            return SeparationOverride ?? 0;
        }
    }

    private int ActualCrossSeparation
    {
        get
        {
            if (TryGetStyleProperty(StylePropertyCrossSeparation, out int separation))
            {
                return separation;
            }

            return CrossSeparationOverride ?? 0;
        }
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var axis = LayoutAxis;

        return axis switch
        {
            Axis.Horizontal => MeasureImplementation<HorizontalAxis>(availableSize),
            Axis.HorizontalReverse => MeasureImplementation<HorizontalReverseAxis>(availableSize),
            Axis.Vertical => MeasureImplementation<VerticalAxis>(availableSize),
            Axis.VerticalReverse => MeasureImplementation<VerticalReverseAxis>(availableSize),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private Vector2 MeasureImplementation<TAxis>(Vector2 availableSize) where TAxis : IAxisImplementation
    {
        _rowIndices.Clear();

        var realAvailableSize = availableSize;
        availableSize = TAxis.SizeToAxis(availableSize);

        // TODO: Round to pixels properly.
        var separation = ActualSeparation;
        var crossSeparation = ActualCrossSeparation;
        var curMainSize = 0f;
        var curCrossSize = 0f;
        var totalCrossSize = 0f;
        var maxMainSize = 0f;
        var firstOnRow = true;

        var equalDesiredSize = _equalSize ? TAxis.SizeToAxis(GetMaxMeasure(realAvailableSize)) : default;
        var countLaidOut = 0;

        foreach (var control in Children)
        {
            Vector2 controlDesiredSize;
            if (_equalSize)
            {
                controlDesiredSize = equalDesiredSize;
            }
            else
            {
                control.Measure(realAvailableSize);
                controlDesiredSize = TAxis.SizeToAxis(control.DesiredSize);
            }

            var controlMainSize = controlDesiredSize.X;
            var spaceTaken = controlMainSize + (firstOnRow ? 0 : separation);
            if (curMainSize + spaceTaken > availableSize.X)
            {
                // We've wrapped.
                RowEnd(lastRow: false);
                curMainSize = controlMainSize;
            }
            else
            {
                curMainSize += spaceTaken;
            }
            curCrossSize = Math.Max(curCrossSize, controlDesiredSize.Y);
            firstOnRow = false;
            countLaidOut += 1;
        }

        RowEnd(lastRow: true);

        _lastMeasureCross = totalCrossSize;

        return TAxis.SizeFromAxis(new Vector2(maxMainSize, totalCrossSize));

        void RowEnd(bool lastRow)
        {
            maxMainSize = Math.Max(maxMainSize, curMainSize);
            totalCrossSize += curCrossSize;
            if (!lastRow)
                totalCrossSize += crossSeparation;
            _rowIndices.Add((countLaidOut, curCrossSize));
            curCrossSize = 0;
        }
    }

    private Vector2 GetMaxDesired()
    {
        var vec = Vector2.Zero;

        foreach (var child in Children)
        {
            vec = Vector2.Max(vec, child.DesiredSize);
        }

        return vec;
    }

    private Vector2 GetMaxMeasure(Vector2 availableSize)
    {
        var vec = Vector2.Zero;

        foreach (var child in Children)
        {
            child.Measure(availableSize);
            vec = Vector2.Max(vec, child.DesiredSize);
        }

        return vec;
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var axis = LayoutAxis;

        return axis switch
        {
            Axis.Horizontal => ArrangeImplementation<HorizontalAxis>(finalSize),
            Axis.HorizontalReverse => ArrangeImplementation<HorizontalReverseAxis>(finalSize),
            Axis.Vertical => ArrangeImplementation<VerticalAxis>(finalSize),
            Axis.VerticalReverse => ArrangeImplementation<VerticalReverseAxis>(finalSize),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private Vector2 ArrangeImplementation<TAxis>(Vector2 finalSize) where TAxis : IAxisImplementation
    {
        var realFinalSize = finalSize;
        finalSize = TAxis.SizeToAxis(finalSize);

        var separation = ActualSeparation;
        var crossSeparation = ActualCrossSeparation;

        var baseOffset = Reverse ? new Vector2(0, _lastMeasureCross) : Vector2.Zero;

        var fixedSize = _equalSize ? (Vector2?)GetMaxDesired() : null;

        var start = 0;
        for (var i = 0; i < _rowIndices.Count; i++)
        {
            var (endIndex, cross) = _rowIndices[i];

            if (Reverse)
            {
                baseOffset.Y -= cross;
            }

            var box = TAxis.BoxFromAxis(UIBox2.FromDimensions(baseOffset, finalSize with { Y = cross }), realFinalSize);

            BoxContainer.LayOutItems<TAxis>(
                box.TopLeft,
                box.Size,
                (BoxContainer.AlignMode)_justification,
                Children,
                start,
                endIndex,
                separation,
                fixedSize);

            start = endIndex;

            if (Reverse)
            {
                baseOffset.Y -= crossSeparation;
            }
            else
            {
                baseOffset.Y = baseOffset.Y + cross + crossSeparation;
            }
        }

        return realFinalSize;
    }

    /// <summary>
    /// Justification values for <see cref="WrapContainer.Justification"/>.
    /// </summary>
    public enum ItemJustification : byte
    {
        // These MUST match the values in BoxContainer.
        Begin = BoxContainer.AlignMode.Begin,
        Center = BoxContainer.AlignMode.Center,
        End = BoxContainer.AlignMode.End
    }
}
