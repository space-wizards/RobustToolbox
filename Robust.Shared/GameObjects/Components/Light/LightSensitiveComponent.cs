using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;
using System;


namespace Robust.Shared.GameObjects;

/// <summary>
///     Marks component that receive illumination from <see cref="SharedPointLightComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedLightSensitiveSystem))]
public sealed partial class LightSensitiveComponent : Component
{

    /// <summary>
    ///     Current illumination value as a percentage.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float LightLevel = 0f;
    
    /// <summary>
    ///     When this Entity last had its light level evaluated. 
    ///To prevent multiple expensive updates per tick.
    ////// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastUpdate = TimeSpan.Zero;

}
