using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls;

/// <summary>
/// A container that lays its children out sequentially along a major axis, <see cref="Orientation"/>.
/// </summary>
/// <example>
/// <code>
/// &lt;BoxContainer Orientation="Vertical"&gt;
///     &lt;Label Text="Above" /&gt;
///     &lt;Label Text="Below" /&gt;
/// &lt;/BoxContainer&gt;
/// </code>
/// <code>
/// var container = new BoxContainer
/// {
///     Orientation = LayoutOrientation.Vertical,
///     Children =
///     {
///         new Label { Text = "Above" },
///         new Label { Text = "Below" }
///     },
/// };
/// </code>
/// </example>
/// <remarks>
/// Use <see cref="WrapContainer"/> if you need wrapping along a minor axis.
/// </remarks>
[Virtual]
public class BoxContainer : Container
{
    /// <summary>
    /// Style property modifying <see cref="ActualSeparation" />, which is the amount of space between children in the
    /// BoxContainer.
    /// </summary>
    /// <example>
    /// <code>
    /// Element&lt;BoxContainer&gt;()
    ///     .Property(BoxContainer.StylePropertySeparation, 8);
    /// </code>
    /// </example>
    /// <remarks>
    /// This is overridden by <see cref="SeparationOverride" />
    /// </remarks>
    public const string StylePropertySeparation = "separation";

    /// <summary>
    /// Style property modifying <see cref="Orientation" />, which decides the major axis for children to be laid out on.
    /// </summary>
    /// <example>
    /// <code>
    /// Element&lt;BoxContainer&gt;()
    ///     .Prop(BoxContainer.StylePropertyOrientation, BoxContainer.LayoutOrientation.Vertical)
    /// </code>
    /// </example>
    /// <remarks>
    /// This is overridden by <see cref="Orientation" />
    /// </remarks>
    public const string StylePropertyOrientation = "orientation";

    /// <summary>
    /// Style property modifying <see cref="Align"/>, which decides how to layout children along the major axis when there is extra space.
    /// </summary>
    /// <example>
    /// <code>
    /// Element&lt;BoxContainer&gt;()
    ///     .Prop(BoxContainer.StylePropertyAlignMode, BoxContainer.AlignMode.Center)
    /// </code>
    /// </example>
    /// <remarks>
    /// This is along the major axis, not the minor axis. If the Orientation is Vertical and Align is Center, it will
    /// align children to the center of the vertical axis.
    /// </remarks>
    /// <remarks>
    /// This is overridden by <see cref="Align" />
    /// </remarks>
    public const string StylePropertyAlignMode = "align-mode";


    /// <summary>
    /// The alignment of child controls <b>along the major axis</b>, defined by <see cref="Orientation"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;BoxContainer Align="Center"&gt;
    ///     &lt;Label Text="Child" /&gt;
    /// &lt;/BoxContainer&gt;
    /// </code>
    /// <code>
    /// var container = new BoxContainer
    /// {
    ///     Align = AlignMode.Center,
    ///     Children =
    ///     {
    ///         new Label { Text = "Child" }
    ///     },
    /// };
    /// </code>
    /// </example>
    /// <param name="value">Overrides <see cref="StylePropertyAlignMode" /> and the default, <see cref="AlignMode.Begin" />, if non-null.</param>
    [NotNull]
    public AlignMode? Align
    {
        get => field ?? StylePropertyDefault(StylePropertyAlignMode, AlignMode.Begin);
        set
        {
            if (field == value)
                return;

            field = value;
            InvalidateArrange();
        }
    }

    /// <summary>
    /// The orientation of the major axis that child controls are laid down along.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;BoxContainer Orientation="Vertical"&gt;
    ///     &lt;Label Text="Above" /&gt;
    ///     &lt;Label Text="Below" /&gt;
    /// &lt;/BoxContainer&gt;
    /// </code>
    /// <code>
    /// var container = new BoxContainer
    /// {
    ///     Orientation = LayoutOrientation.Vertical,
    ///     Children =
    ///     {
    ///         new Label { Text = "Above" },
    ///         new Label { Text = "Below" }
    ///     },
    /// };
    /// </code>
    /// </example>
    /// <param name="value">Overrides <see cref="StylePropertyOrientation" /> and the default, <see cref="LayoutOrientation.Horizontal" />, if non-null.</param>
    [NotNull]
    public LayoutOrientation? Orientation
    {
        get => field ?? StylePropertyDefault(StylePropertyOrientation, LayoutOrientation.Horizontal);
        set
        {
            if (field == value)
                return;

            field = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// The separation/gap between the child elements along the major axis.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;BoxContainer Separation="2"&gt;
    ///     &lt;Label Text="Left" /&gt;
    ///     &lt;Label Text="Right" /&gt;
    /// &lt;/BoxContainer&gt;
    /// </code>
    /// <code>
    /// var container = new BoxContainer
    /// {
    ///     Separation = 2,
    ///     Children =
    ///     {
    ///         new Label { Text = "Left" },
    ///         new Label { Text = "Right" }
    ///     },
    /// };
    /// </code>
    /// </example>
    /// <param name="value">Overrides <see cref="StylePropertySeparation" /> and the default, 0, if non-null.</param>
    [NotNull]
    public int? Separation
    {
        get => field ?? StylePropertyDefault(StylePropertySeparation, 0);
        set
        {
            if (field == value)
                return;

            field = value;
            InvalidateMeasure();
        }
    }

    /// <seealso cref="Separation"/>
    [Obsolete("Use BoxContainer.Separation directly instead.")]
    public int? SeparationOverride
    {
        get => Separation;
        set => Separation = value;
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        return Orientation == LayoutOrientation.Vertical
            ? MeasureItems<VerticalAxis>(availableSize)
            : MeasureItems<HorizontalAxis>(availableSize);
    }

    private Vector2 MeasureItems<TAxis>(Vector2 availableSize) where TAxis : IAxisImplementation
    {
        availableSize = TAxis.SizeToAxis(availableSize);

        // Account for separation.
        var separation = Separation.Value * (Children.Count(c => c.Visible) - 1);
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
        var separation = Separation.Value;

        if (Orientation == LayoutOrientation.Vertical)
        {
            LayOutItems<VerticalAxis>(default, finalSize, Align.Value, Children, 0, ChildCount, separation);
        }
        else
        {
            LayOutItems<HorizontalAxis>(default, finalSize, Align.Value, Children, 0, ChildCount, separation);
        }

        return finalSize;
    }

    internal static void LayOutItems<TAxis>(
        Vector2 baseOffset,
        Vector2 finalSize,
        AlignMode align,
        OrderedChildCollection children,
        int start,
        int end,
        float separation,
        Vector2? fixedSize = null)
        where TAxis : IAxisImplementation
    {
        var realFinalSize = finalSize;
        finalSize = TAxis.SizeToAxis(finalSize);
        fixedSize = fixedSize == null ? null : TAxis.SizeToAxis(fixedSize.Value);

        var visibleChildCount = 0;
        for (var i = start; i < end; i++)
        {
            if (children[i].Visible)
                visibleChildCount += 1;
        }

        var stretchAvail = finalSize.X;
        stretchAvail -= separation * (visibleChildCount - 1);
        stretchAvail = Math.Max(0, stretchAvail);

        // Step one: figure out the sizes of all our children and whether they want to stretch.
        var sizeList = new List<(Control control, float size, bool stretch)>(visibleChildCount);
        var totalStretchRatio = 0f;
        for (var i = start; i < end; i++)
        {
            var child = children[i];
            if (!child.Visible)
                continue;

            bool stretch = TAxis.GetMainExpandFlag(child);
            if (!stretch)
            {
                var measuredSize = fixedSize ?? TAxis.SizeToAxis(child.DesiredSize);
                var size = measuredSize.X;
                size = Math.Clamp(size, 0, stretchAvail);
                stretchAvail -= size;
                sizeList.Add((child, size, false));
            }
            else
            {
                totalStretchRatio += child.SizeFlagsStretchRatio;
                sizeList.Add((child, 0, true));
            }
        }

        stretchAvail = Math.Max(0, stretchAvail);

        // Step two: allocate space for all the stretchable children.
        float offset = 0;
        var anyStretch = totalStretchRatio > 0;
        if (anyStretch)
        {
            // We will treat stretching children that fail to reach their desired size as non-stretching.
            // This then requires all stretching children to be re-stretched
            bool stretchAvailChanged = true;
            while (stretchAvailChanged)
            {
                stretchAvailChanged = false;
                for (var i = 0; i < sizeList.Count; i++)
                {
                    var (control, _, stretch) = sizeList[i];
                    if (!stretch)
                        continue;

                    var share = stretchAvail * control.SizeFlagsStretchRatio / totalStretchRatio;
                    var measuredSize = fixedSize ?? TAxis.SizeToAxis(control.DesiredSize);
                    var desired = measuredSize.X;
                    if (share >= desired)
                    {
                        sizeList[i] = (control, share, true);
                        continue;
                    }

                    // Insufficient space -> treat as non-stretching.
                    sizeList[i] = (control, Math.Min(stretchAvail, desired), false);
                    stretchAvail = Math.Max(0, stretchAvail - desired);
                    totalStretchRatio -= control.SizeFlagsStretchRatio;
                    stretchAvailChanged = true;
                }
            }
        }
        else
        {
            // No stretching children -> offset the children based on the alignment.
            switch (align)
            {
                case AlignMode.Begin:
                    break;
                case AlignMode.Center:
                    offset = stretchAvail / 2;
                    break;
                case AlignMode.End:
                    offset = stretchAvail;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Step three: actually lay them out one by one.
        var first = true;
        foreach (var (control, size, _) in sizeList)
        {
            if (!first)
            {
                offset += separation;
            }

            first = false;

            var targetBox = TAxis.BoxFromAxis(new UIBox2(offset, 0, offset + size, finalSize.Y), realFinalSize);

            targetBox = targetBox.Translated(baseOffset);

            control.Arrange(targetBox);

            offset += size;
        }
    }

    public enum AlignMode : byte
    {
        /// <summary>
        ///     Controls are laid out from the begin of the box container.
        /// </summary>
        Begin = 0,

        /// <summary>
        ///     Controls are laid out from the center of the box container.
        /// </summary>
        Center = 1,

        /// <summary>
        ///     Controls are laid out from the end of the box container.
        /// </summary>
        End = 2
    }

    /// <summary>
    /// Orientation for a box container.
    /// </summary>
    public enum LayoutOrientation : byte
    {
        /// <summary>
        /// Controls are laid out horizontally, left to right.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Controls are laid out vertically, top to bottom.
        /// </summary>
        Vertical
    }
}
