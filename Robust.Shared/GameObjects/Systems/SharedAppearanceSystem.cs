using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects;

internal abstract class SharedAppearanceSystem : EntitySystem
{
    public virtual void MarkDirty(AppearanceComponent component) {}
}

[Serializable, NetSerializable]
public sealed class AppearanceComponentState : ComponentState
{
    public readonly Dictionary<object, object> Data;

    public AppearanceComponentState(Dictionary<object, object> data)
    {
        Data = data;
    }
}
