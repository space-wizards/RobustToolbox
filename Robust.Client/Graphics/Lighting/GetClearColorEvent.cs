using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics;

/// <summary>
/// Raised by the engine if content wishes to override the default clear color.
/// </summary>
[ByRefEvent]
public record struct GetClearColorEvent
{
    public Color? Color;
}
