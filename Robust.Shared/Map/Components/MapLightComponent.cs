using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Map.Components;

/// <summary>
/// Controls per-map lighting values.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MapLightComponent : Component
{
    public static readonly Color DefaultColor = Color.FromSrgb(Color.Black);

    /// <summary>
    /// Ambient light. This is in linear-light, i.e. when providing a fixed colour, you must use Color.FromSrgb(Color.Black)!
    /// </summary>
    [DataField]
    public Color AmbientLightColor = Color.FromSrgb(Color.Black);
}
