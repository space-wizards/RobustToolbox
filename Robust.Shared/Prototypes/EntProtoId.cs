using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Toolshed.TypeParsers;

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
public readonly record struct EntProtoId(string Id) : IEquatable<string>, IComparable<EntProtoId>, IAsType<string>,
    IAsType<ProtoId<EntityPrototype>>
{
    public static implicit operator string(EntProtoId protoId)
    {
        return protoId.Id;
    }

    public static implicit operator EntProtoId(EntityPrototype proto)
    {
        return new EntProtoId(proto.ID);
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

    string IAsType<string>.AsType() => Id;

    ProtoId<EntityPrototype> IAsType<ProtoId<EntityPrototype>>.AsType() => new(Id);

    public override string ToString() => Id ?? string.Empty;
}

/// <inheritdoc cref="EntProtoId"/>
[Serializable]
public readonly record struct EntProtoId<T>(string Id) : IEquatable<string>, IComparable<EntProtoId> where T : IComponent, new()
{
    public static implicit operator string(EntProtoId<T> protoId)
    {
        return protoId.Id;
    }

    public static implicit operator EntProtoId(EntProtoId<T> protoId)
    {
        return new EntProtoId(protoId.Id);
    }

    public static implicit operator EntProtoId<T>(string id)
    {
        return new EntProtoId<T>(id);
    }

    public static implicit operator EntProtoId<T>?(string? id)
    {
        return id == null ? default(EntProtoId<T>?) : new EntProtoId<T>(id);
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

    public T Get(IPrototypeManager? prototypes, IComponentFactory compFactory)
    {
        prototypes ??= IoCManager.Resolve<IPrototypeManager>();
        var proto = prototypes.Index(this);
        if (!proto.TryGetComponent(out T? comp, compFactory))
        {
            throw new ArgumentException($"{nameof(EntityPrototype)} {proto.ID} has no {nameof(T)}");
        }

        return comp;
    }

    public bool TryGet([NotNullWhen(true)] out T? comp, IPrototypeManager? prototypes, IComponentFactory compFactory)
    {
        prototypes ??= IoCManager.Resolve<IPrototypeManager>();
        var proto = prototypes.Index(this);
        return proto.TryGetComponent(out comp, compFactory);
    }
}
