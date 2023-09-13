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
public readonly record struct EntProtoId(string Id)
{
    public static implicit operator string(EntProtoId protoId)
    {
        return protoId.Id;
    }

    public static implicit operator EntProtoId(string id)
    {
        return new EntProtoId(id);
    }
}
