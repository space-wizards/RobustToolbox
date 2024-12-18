using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System;

namespace Robust.Shared.Audio;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class SoundSpecifier
{
    [DataField("params")]
    public AudioParams Params { get; set; } = AudioParams.Default;
}

[Serializable, NetSerializable]
public sealed partial class SoundPathSpecifier : SoundSpecifier
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
}

[Serializable, NetSerializable]
public sealed partial class SoundCollectionSpecifier : SoundSpecifier
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
}
