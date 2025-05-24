using System;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Audio;

/// <summary>
/// Represents a path to a sound resource, either as a literal path or as a collection ID and index.
/// </summary>
/// <seealso cref="ResolvedPathSpecifier"/>
/// <seealso cref="ResolvedCollectionSpecifier"/>
[Serializable, NetSerializable]
public abstract partial class ResolvedSoundSpecifier {
    [Obsolete("String literals for sounds are deprecated, use a SoundSpecifier or ResolvedSoundSpecifier as appropriate instead")]
    public static implicit operator ResolvedSoundSpecifier(string s) => new ResolvedPathSpecifier(s);
    [Obsolete("String literals for sounds are deprecated, use a SoundSpecifier or ResolvedSoundSpecifier as appropriate instead")]
    public static implicit operator ResolvedSoundSpecifier(ResPath s) => new ResolvedPathSpecifier(s);

    /// <summary>
    /// Returns whether <c>s</c> is null, or if it contains an empty path/collection ID.
    /// </summary>
    public static bool IsNullOrEmpty(ResolvedSoundSpecifier? s) {
        return s switch {
            null => true,
            ResolvedPathSpecifier path => path.Path.ToString() == "",
            ResolvedCollectionSpecifier collection => string.IsNullOrEmpty(collection.Collection),
            _ => throw new ArgumentOutOfRangeException("s", s, "argument is not a ResolvedPathSpecifier or a ResolvedCollectionSpecifier"),
        };
    }
}

/// <summary>
/// Represents a path to a sound resource as a literal path.
/// </summary>
/// <seealso cref="ResolvedCollectionSpecifier"/>
[Serializable, NetSerializable]
public sealed partial class ResolvedPathSpecifier : ResolvedSoundSpecifier {
    /// <summary>
    /// The resource path of the sound.
    /// </summary>
    public ResPath Path { get; private set; }

    override public string ToString() =>
        $"ResolvedPathSpecifier({Path})";

    [UsedImplicitly]
    private ResolvedPathSpecifier()
    {
    }
    public ResolvedPathSpecifier(ResPath path)
    {
        Path = path;
    }
    public ResolvedPathSpecifier(string path) : this(new ResPath(path))
    {
    }
}

/// <summary>
/// Represents a path to a sound resource as a collection ID and index.
/// </summary>
/// <seealso cref="ResolvedPathSpecifier"/>
[Serializable, NetSerializable]
public sealed partial class ResolvedCollectionSpecifier : ResolvedSoundSpecifier {
    /// <summary>
    /// The ID of the <see cref="SoundCollectionPrototype">sound collection</see> to look up.
    /// </summary>
    public ProtoId<SoundCollectionPrototype>? Collection { get; private set; }
    /// <summary>
    /// The index of the file in the associated sound collection to play.
    /// </summary>
    public int Index { get; private set; }

    override public string ToString() =>
        $"ResolvedCollectionSpecifier({Collection}, {Index})";

    [UsedImplicitly]
    private ResolvedCollectionSpecifier()
    {
    }

    public ResolvedCollectionSpecifier(string collection, int index)
    {
        Collection = collection;
        Index = index;
    }
}
