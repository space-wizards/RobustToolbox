using System.Numerics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface;

/// <summary>
/// Defines an axis that certain controls can be laid out along.
/// </summary>
/// <seealso cref="IAxisImplementation"/>
public enum Axis : byte
{
    /// <summary>
    /// Items are laid out left to right.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Items are laid out right to left.
    /// </summary>
    HorizontalReverse,

    /// <summary>
    /// Items are laid out top to bottom.
    /// </summary>
    Vertical,

    /// <summary>
    /// Items are laid out bottom to top.
    /// </summary>
    VerticalReverse,
}

/// <summary>
/// Interface that implements the rules of an <see cref="Axis"/>.
/// </summary>
/// <remarks>
/// <para>
/// To make it easier to write code that supports all 4 layout axis, layout code is advised to use generics over this
/// type and its implementors.
/// </para>
/// <para>
/// An axis has a "main" and a "cross" axis. For example,
/// <see cref="HorizontalAxis"/> has the main axis go left to right, and the cross axis go top to bottom.
/// </para>
/// <para>
/// The functions in this interface primarily allow converting between "UI space" (the normal UI coordinate system) and
/// "axis space" (same as UI space for <see cref="HorizontalAxis"/>). This allows you to write all code as if you're
/// doing only horizontal layout, but automatically have it work on all axis.
/// </para>
/// </remarks>
/// <seealso cref="HorizontalAxis"/>
/// <seealso cref="HorizontalReverseAxis"/>
/// <seealso cref="VerticalAxis"/>
/// <seealso cref="VerticalReverseAxis"/>
public interface IAxisImplementation
{
    //
    // To/from axis space conversions
    //

    /// <summary>
    /// Convert a size value (e.g. from <see cref="Control.DesiredSize"/>) from UI space to axis space.
    /// </summary>
    static abstract Vector2 SizeToAxis(Vector2 size);

    /// <summary>
    /// Convert a size value (e.g. for <see cref="Control.Measure"/>) from axis space to UI space.
    /// </summary>
    static abstract Vector2 SizeFromAxis(Vector2 size);

    /// <summary>
    /// Convert a box (e.g. for <see cref="Control.Arrange"/>) from axis space to UI space.
    /// </summary>
    /// <param name="box">The box to convert, in axis space.</param>
    /// <param name="spaceSize">The amount of space, in UI space, that the layout is happening relative to.</param>
    static abstract UIBox2 BoxFromAxis(UIBox2 box, Vector2 spaceSize);

    //
    // Control
    //

    /// <summary>
    /// Gets the "expand flag" (<see cref="Control.HorizontalExpand"/> or <see cref="Control.VerticalExpand"/>) for a
    /// control that is appropriate for the main axis.
    /// </summary>
    static abstract bool GetMainExpandFlag(Control control);
}

/// <summary>
/// Axis implementation for <see cref="Axis.Horizontal"/>.
/// </summary>
public struct HorizontalAxis : IAxisImplementation
{
    public static Vector2 SizeToAxis(Vector2 size)
    {
        return size;
    }

    public static Vector2 SizeFromAxis(Vector2 size)
    {
        return size;
    }

    public static UIBox2 BoxFromAxis(UIBox2 box, Vector2 spaceSize)
    {
        return box;
    }

    public static bool GetMainExpandFlag(Control control)
    {
        return control.HorizontalExpand;
    }
}

/// <summary>
/// Axis implementation for <see cref="Axis.HorizontalReverse"/>.
/// </summary>
public struct HorizontalReverseAxis : IAxisImplementation
{
    public static Vector2 SizeToAxis(Vector2 size)
    {
        return size;
    }

    public static Vector2 SizeFromAxis(Vector2 size)
    {
        return size;
    }

    public static UIBox2 BoxFromAxis(UIBox2 box, Vector2 spaceSize)
    {
        return new UIBox2(spaceSize.X - box.Right, box.Top, spaceSize.X - box.Left, box.Bottom);
    }

    public static bool GetMainExpandFlag(Control control)
    {
        return control.HorizontalExpand;
    }
}

/// <summary>
/// Axis implementation for <see cref="Axis.Vertical"/>.
/// </summary>
public struct VerticalAxis : IAxisImplementation
{
    public static Vector2 SizeToAxis(Vector2 size)
    {
        return new Vector2(size.Y, size.X);
    }

    public static Vector2 SizeFromAxis(Vector2 size)
    {
        return new Vector2(size.Y, size.X);
    }

    public static UIBox2 BoxFromAxis(UIBox2 box, Vector2 spaceSize)
    {
        return new UIBox2(box.Top, box.Left, box.Bottom, box.Right);
    }

    public static bool GetMainExpandFlag(Control control)
    {
        return control.VerticalExpand;
    }
}

/// <summary>
/// Axis implementation for <see cref="Axis.VerticalReverse"/>.
/// </summary>
public struct VerticalReverseAxis : IAxisImplementation
{
    public static Vector2 SizeToAxis(Vector2 size)
    {
        return new Vector2(size.Y, size.X);
    }

    public static Vector2 SizeFromAxis(Vector2 size)
    {
        return new Vector2(size.Y, size.X);
    }

    public static UIBox2 BoxFromAxis(UIBox2 box, Vector2 spaceSize)
    {
        return new UIBox2(box.Top, spaceSize.Y - box.Right, box.Bottom, spaceSize.Y - box.Left);
    }

    public static bool GetMainExpandFlag(Control control)
    {
        return control.VerticalExpand;
    }
}

