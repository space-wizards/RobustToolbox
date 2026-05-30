using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls;

/// <summary>
/// A container that lays its children out sequentially along a main axis, <see cref="Orientation"/>.
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
///     }
/// };
/// </code>
/// </example>
/// <remarks>
/// Use <see cref="WrapContainer"/> if you need wrapping along a cross axis.
/// </remarks>
[Virtual]
public class BoxContainer : Container
{
    /// <summary>
    /// Style property modifying <see cref="Separation"/>, which is the amount of space between children in the
    /// BoxContainer.
    /// </summary>
    /// <example>
    /// <code>
    /// Element&lt;BoxContainer&gt;()
    ///     .Property(BoxContainer.StylePropertySeparation, 8);
    /// </code>
    /// </example>
    /// <remarks>
    /// This is overridden by a non-null <see cref="Separation"/> value.
    /// </remarks>
    public const string StylePropertySeparation = "separation";

    /// <summary>
    /// Style property modifying <see cref="Orientation"/>, which decides the main axis for children to be laid out on.
    /// </summary>
    /// <example>
    /// <code>
    /// Element&lt;BoxContainer&gt;()
    ///     .Prop(BoxContainer.StylePropertyOrientation, BoxContainer.LayoutOrientation.Vertical)
    /// </code>
    /// </example>
    /// <remarks>
    /// This is overridden by a non-null <see cref="Orientation"/> value.
    /// </remarks>
    public const string StylePropertyOrientation = "orientation";

    /// <summary>
    /// Style property modifying <see cref="Align"/>, which decides how to layout children along the main axis when there is extra space.
    /// </summary>
    /// <example>
    /// If the Orientation is Vertical and Align is Center, it will align children to the center of the vertical axis.
    /// </example>
    /// <example>
    /// <code>
    /// Element&lt;BoxContainer&gt;()
    ///     .Prop(BoxContainer.StylePropertyAlignMode, BoxContainer.AlignMode.Center)
    /// </code>
    /// </example>
    /// <remarks>
    /// This is along the main axis, not the cross axis. Yes, this what's commonly called "justify".
    /// </remarks>
    /// <remarks>
    /// This is overridden by a non-null <see cref="Align"/> value.
    /// </remarks>
    public const string StylePropertyAlignMode = "align-mode";

    /// <summary>
    /// The alignment of child controls <b>along the main axis</b>, defined by <see cref="Orientation"/>.
    /// </summary>
    /// <example>
    /// If the Orientation is Vertical and Align is Center, it will align children to the center of the vertical axis.
    /// </example>
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
    ///     }
    /// };
    /// </code>
    /// </example>
    /// <remarks>
    /// This is along the main axis, not the cross axis. Yes, this what's commonly called "justify".
    /// </remarks>
    /// <param name="value">Overrides <see cref="StylePropertyAlignMode"/> and the default, <see cref="AlignMode.Begin"/>, if non-null.</param>
    [NotNull, ViewVariables(VVAccess.ReadWrite)]
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
    /// The orientation/direction of the main axis that child controls are laid down along.
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
    ///     }
    /// };
    /// </code>
    /// </example>
    /// <param name="value">Overrides <see cref="StylePropertyOrientation"/> and the default, <see cref="LayoutOrientation.Horizontal"/>, if non-null.</param>
    [NotNull, ViewVariables(VVAccess.ReadWrite)]
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
    /// The separation/gap between the child elements along the main axis.
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
    ///     }
    /// };
    /// </code>
    /// </example>
    /// <param name="value">Overrides <see cref="StylePropertySeparation"/> and the default, 0, if non-null.</param>
    [NotNull, ViewVariables(VVAccess.ReadWrite)]
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

    /// <summary>
    /// Overrides the separation between children.
    /// </summary>
    /// <remark>
    /// Use <see cref="Separation"/> instead.
    /// </remark>
    /// <remark>
    /// Checking the nullability of this value no longer tells if it has been overridden or not, as the value is NotNull.
    /// </remark>
    [Obsolete("Use BoxContainer.Separation directly instead.")]
    [NotNull]
    public int? SeparationOverride
    {
        [Obsolete("This is now NotNull and cannot be used to test for overridden separation. Switch to BoxContainer.Separation.", true)]
        get => Separation;
        set => Separation = value;
    }

    /// <inheritdoc/>
    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        return Orientation == LayoutOrientation.Vertical
            ? MeasureItems<VerticalAxis>(availableSize)
            : MeasureItems<HorizontalAxis>(availableSize);
    }

    /// <summary>
    /// Measures the desired size required to fit all visible children and the separation between them.
    /// </summary>
    /// <param name="availableSize">The available size to measure against.</param>
    /// <typeparam name="TAxis">The main axis/orientation.</typeparam>
    /// <returns>The desired size for the measured children.</returns>
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

    /// <inheritdoc/>
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

    /// <summary>
    /// Arranges a range of child controls sequentially along an axis.
    /// </summary>
    /// <param name="baseOffset">Initial offset for first child position.</param>
    /// <param name="finalSize">The final available size of the arranged region.</param>
    /// <param name="align">Mode to align children along the main axis.</param>
    /// <param name="children">The children to lay out.</param>
    /// <param name="start">Child index to start from.</param>
    /// <param name="end">Child index to end at.</param>
    /// <param name="separation">Amount of separation between children.</param>
    /// <param name="fixedSize">A fixed size used for each child's size rather than their individual measures.</param>
    /// <typeparam name="TAxis">The main axis/orientation.</typeparam>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="align"/> is outside <see cref="AlignMode"/>.</exception>
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
                    throw new ArgumentOutOfRangeException(nameof(align), align, "Align is out of range.");
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

    /// <summary>
    /// The alignment of child controls <b>along the main axis</b>, dependent on a <see cref="LayoutOrientation"/>.
    /// </summary>
    /// <remarks>
    /// This is along the main axis, not the cross axis. Yes, this what's commonly called "justify".
    /// If the Orientation is Vertical and Align is Center, it will align children to the center of the vertical axis.
    /// </remarks>
    /// <remarks>Defaults to <see cref="AlignMode.Begin"/></remarks>
    public enum AlignMode : byte
    {
        /// <summary>
        /// Child controls are laid out from the start/beginning.
        /// </summary>
        /// <example>
        /// <see cref="LayoutOrientation.Vertical"/> means start is the top. <br />
        /// <see cref="LayoutOrientation.Horizontal"/> means start is the left.
        /// </example>
        /// <remarks>The default <see cref="AlignMode"/>.</remarks>
        Begin = 0,

        /// <summary>
        /// Child controls are laid out from the center.
        /// </summary>
        /// <example>
        /// <see cref="LayoutOrientation.Vertical"/> means start is the center vertically. <br />
        /// <see cref="LayoutOrientation.Horizontal"/> means start is the center horizontally.
        /// </example>
        Center = 1,

        /// <summary>
        /// Child controls are laid out from the end.
        /// </summary>
        /// <example>
        /// <see cref="LayoutOrientation.Vertical"/> means start is the bottom. <br />
        /// <see cref="LayoutOrientation.Horizontal"/> means start is the right.
        /// </example>
        End = 2
    }

    /// <summary>
    /// The orientation of the main axis that child controls are laid out along.
    /// </summary>
    /// <remarks>
    /// <see cref="AlignMode"/>'s meaning changes based on the orientation.
    /// </remarks>
    /// <remarks>Defaults to <see cref="LayoutOrientation.Horizontal"/></remarks>
    public enum LayoutOrientation : byte
    {
        /// <summary>
        /// Controls are laid out horizontally, left to right.
        /// </summary>
        /// <example>
        /// <see cref="AlignMode.Begin"/> becomes the left. <br/>
        /// <see cref="AlignMode.Center"/> becomes the center horizontally. <br/>
        /// <see cref="AlignMode.End"/> becomes the right.
        /// </example>
        /// <remarks>The default <see cref="LayoutOrientation"/></remarks>
        Horizontal,

        /// <summary>
        /// Controls are laid out vertically, top to bottom.
        /// </summary>
        /// <example>
        /// <see cref="AlignMode.Begin"/> becomes the top. <br/>
        /// <see cref="AlignMode.Center"/> becomes the center vertically. <br/>
        /// <see cref="AlignMode.End"/> becomes the bottom.
        /// </example>
        Vertical
    }
}
