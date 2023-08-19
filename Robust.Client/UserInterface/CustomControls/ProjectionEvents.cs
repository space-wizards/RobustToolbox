using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using System.Numerics;

namespace Robust.Client.UserInterface.CustomControls;

/// <summary>
/// An event used to reverse distortion effects applied by shaders.
/// Used to find the map position that visible pixels originate from so that severe distortion shaders do not make interaction nigh-impossible.
/// </summary>
[ByRefEvent]
public record struct PixelToMapEvent(Vector2 LocalPosition, IViewportControl Control, IClydeViewport Viewport)
{
    /// <summary>
    /// The local position of the pixel within the <see cref="Control"/> that we are trying to convert to a map position.
    /// </summary>
    public readonly Vector2 LocalPosition = LocalPosition;

    /// <summary>
    /// The original (or WIP) location of the pixel within the <see cref="Control"/> that we are trying to convert to a map position.
    /// Used as the output of the event.
    /// </summary>
    public Vector2 VisiblePosition = LocalPosition;

    /// <summary>
    /// The control the pixel we are considering is located within.
    /// </summary>
    public readonly IViewportControl Control = Control;

    /// <summary>
    /// The viewport being displayed by the control we are considering.
    /// </summary>
    public readonly IClydeViewport Viewport = Viewport;
}
