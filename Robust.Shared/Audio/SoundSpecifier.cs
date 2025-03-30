using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using System;

namespace Robust.Shared.Audio;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class SoundSpecifier : IEquatable<SoundSpecifier>
{
    [DataField]
    public AudioParams Params { get; set; } = AudioParams.Default;

    public virtual bool Equals(SoundSpecifier? other)
    {
        if (other == null)
            return false;

        return Params.Equals(other.Params);
    }

    public abstract override bool Equals(object? obj);

    public override int GetHashCode()
    {
        return Params.GetHashCode();
    }
}

[Serializable, NetSerializable]
public sealed partial class SoundPathSpecifier : SoundSpecifier, IEquatable<SoundPathSpecifier>
{
    public const string Node = "path";

    [DataField(Node, customTypeSerializer: typeof(ResPathSerializer), required: true)]
    public ResPath Path { get; private set; }

    override public string ToString() =>
        $"SoundPathSpecifier({Path})";

    [UsedImplicitly]
    private SoundPathSpecifier()
    {
    }

    public SoundPathSpecifier(string path, AudioParams? @params = null) : this(new ResPath(path), @params)
    {
    }

    public SoundPathSpecifier(ResPath path, AudioParams? @params = null)
    {
        Path = path;
        if (@params.HasValue)
            Params = @params.Value;
    }

    public bool Equals(SoundPathSpecifier? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Path.Equals(other.Path) && base.Equals(other);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is SoundPathSpecifier other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Path);
    }
}

[Serializable, NetSerializable]
public sealed partial class SoundCollectionSpecifier : SoundSpecifier, IEquatable<SoundCollectionSpecifier>
{
    public const string Node = "collection";

    [DataField(Node, customTypeSerializer: typeof(PrototypeIdSerializer<SoundCollectionPrototype>), required: true)]
    public string? Collection { get; private set; }

    override public string ToString() =>
        $"SoundCollectionSpecifier({Collection})";

    [UsedImplicitly]
    public SoundCollectionSpecifier() { }

    public SoundCollectionSpecifier(string collection, AudioParams? @params = null)
    {
        Collection = collection;
        if (@params.HasValue)
            Params = @params.Value;
    }

    public bool Equals(SoundCollectionSpecifier? other)
    {
        if (other == null)
            return false;

        return Collection == other.Collection;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is SoundCollectionSpecifier other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Collection);
    }
}
