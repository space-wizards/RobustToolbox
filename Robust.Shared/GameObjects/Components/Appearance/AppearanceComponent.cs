using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

/// <summary>
///     The appearance component allows game logic to be more detached from the actual visuals of an entity such as 2D
///     sprites, 3D, particles, lights...
///     It does this with a "data" system. Basically, code writes data to the component, and the component will use
///     prototype-based configuration to change the actual visuals.
///     The data works using a simple key/value system. It is recommended to use enum keys to prevent errors.
///     Visualization works client side with overrides of the <c>AppearanceVisualizer</c> class.
/// </summary>
[NetworkedComponent]
public abstract class AppearanceComponent : Component
{
    [ViewVariables] internal bool AppearanceDirty;

    [ViewVariables] internal Dictionary<Enum, object> AppearanceData = new();

    [Dependency] private readonly IEntitySystemManager _sysMan = default!;

    [Obsolete("Use SharedAppearanceSystem instead")]
    public void SetData(Enum key, object value)
    {
        _sysMan.GetEntitySystem<SharedAppearanceSystem>().SetData(Owner, key, value, this);
    }

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
