using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

/// <summary>
///     The appearance component allows game logic to be more detached from the actual visuals of an entity such as 2D
///     sprites, 3D, particles, lights...
///     It does this with a "data" system. Basically, code writes data to the component, and the component will use
///     prototype-based configuration to change the actual visuals.
///     The data works using a simple key/value system. It is recommended to use enum keys to prevent errors.
///     Visualization works client side with derivatives of the <see cref="Robust.Client.GameObjects.VisualizerSystem">VisualizerSystem</see> class and corresponding components.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AppearanceComponent : Component
{
    /// <summary>
    /// Whether or not the appearance needs to be updated.
    /// </summary>
    [ViewVariables] internal bool AppearanceDirty;

    /// <summary>
    /// If true, this entity will have its appearance updated in the next frame update.
    /// </summary>
    /// <remarks>
    /// If an entity is outside of PVS range, this may be false while <see cref="AppearanceDirty"/> is true.
    /// </remarks>
    [ViewVariables] internal bool UpdateQueued;

    [ViewVariables] internal Dictionary<Enum, object> AppearanceData = new();

    [Obsolete("Use SharedAppearanceSystem instead")]
    public bool TryGetData<T>(Enum key, [NotNullWhen(true)] out T data)
    {
        if (AppearanceData.TryGetValue(key, out var dat) && dat is T)
        {
            data = (T)dat;
            return true;
        }

        data = default!;
        return false;
    }
}
