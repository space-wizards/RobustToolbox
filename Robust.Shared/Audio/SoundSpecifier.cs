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

    [Obsolete("Use SharedAudioSystem.GetSound(), or just pass sound specifier directly into SharedAudioSystem.")]
    public abstract string GetSound(IRobustRandom? rand = null, IPrototypeManager? proto = null);
}

[Serializable, NetSerializable]
public sealed partial class SoundPathSpecifier : SoundSpecifier
{
    public const string Node = "path";

    [DataField(Node, customTypeSerializer: typeof(ResPathSerializer), required: true)]
    public ResPath Path { get; private set; }

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

    [Obsolete("Use SharedAudioSystem.GetSound(), or just pass sound specifier directly into SharedAudioSystem.")]
    public override string GetSound(IRobustRandom? rand = null, IPrototypeManager? proto = null)
    {
        return Path.ToString();
    }
}

[Serializable, NetSerializable]
public sealed partial class SoundCollectionSpecifier : SoundSpecifier
{
    public const string Node = "collection";

    [DataField(Node, customTypeSerializer: typeof(PrototypeIdSerializer<SoundCollectionPrototype>), required: true)]
    public string? Collection { get; private set; }

    [UsedImplicitly]
    public SoundCollectionSpecifier() { }

    public SoundCollectionSpecifier(string collection, AudioParams? @params = null)
    {
        Collection = collection;
        if (@params.HasValue)
            Params = @params.Value;
    }

    [Obsolete("Use SharedAudioSystem.GetSound(), or just pass sound specifier directly into SharedAudioSystem.")]
    public override string GetSound(IRobustRandom? rand = null, IPrototypeManager? proto = null)
    {
        if (Collection == null)
            return string.Empty;

        IoCManager.Resolve(ref rand, ref proto);
        var soundCollection = proto.Index<SoundCollectionPrototype>(Collection);
        return rand.Pick(soundCollection.PickFiles).ToString();
    }
}
