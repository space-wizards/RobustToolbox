using System;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Robust.Shared.Prototypes;

/// <summary>
///     Wrapper type for an <see cref="EntityPrototype"/> with a given id.
/// </summary>
/// <param name="Id">The id of the prototype.</param>
/// <remarks>
///     This will be automatically validated by <see cref="EntProtoIdSerializer"/> if used in data fields.
/// </remarks>
/// <remarks><seealso cref="ProtoId{T}"/> for a wrapper of other prototype kinds.</remarks>
[Serializable, NetSerializable]
public readonly record struct EntProtoId(string Id) : IEquatable<string>, IComparable<EntProtoId>
{
    public static implicit operator string(EntProtoId protoId)
    {
        return protoId.Id;
    }

    public static implicit operator EntProtoId(string id)
    {
        return new EntProtoId(id);
    }

    public static implicit operator EntProtoId?(string? id)
    {
        return id == null ? default(EntProtoId?) : new EntProtoId(id);
    }

    public bool Equals(string? other)
    {
        return Id == other;
    }

    public int CompareTo(EntProtoId other)
    {
        return string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public override string ToString() => Id ?? string.Empty;
}
