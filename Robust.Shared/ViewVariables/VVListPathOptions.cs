using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.ViewVariables;

// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public readonly struct VVListPathOptions
{
    public VVAccess MinimumAccess { get; init; }
    public bool ListIndexers { get; init; }
    public int RemoteListLength { get; init; }

    public VVListPathOptions()
    {
        MinimumAccess = VVAccess.ReadOnly;
        ListIndexers = true;
        RemoteListLength = ViewVariablesManager.MaxListPathResponseLength;
    }
}
