using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Audio;

/// <summary>
/// Stores server-side metadata about an audio file.
/// This means you don't need to ship entire audio files with the server.
/// </summary>
[Prototype(ProtoName)]
public sealed class AudioMetadataPrototype : IPrototype
{
    public const string ProtoName = "audioMetadata";

    [IdDataField] public string ID { get; set; } = string.Empty;

    [DataField]
    public TimeSpan Length;
}
