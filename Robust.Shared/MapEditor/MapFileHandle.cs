using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.MapEditor;

[Serializable, NetSerializable]
internal readonly record struct MapFileHandle(Guid Token)
{
    public static MapFileHandle CreateUnique()
    {
        return new MapFileHandle(Guid.NewGuid());
    }
}

