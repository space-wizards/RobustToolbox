using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Audio;

/// <summary>
/// Stores server-side metadata about an audio file.
/// These prototypes get automatically generated when packaging the server,
/// to allow the server to know audio lengths without shipping the large audio files themselves.
/// </summary>
#pragma warning disable RA0035 // Redundant Prototype Type specification
[Prototype(ProtoName)]
#pragma warning restore RA0035 // Redundant Prototype Type specification
public sealed partial class AudioMetadataPrototype : IPrototype
{
    public const string ProtoName = "audioMetadata";

    [IdDataField] public string ID { get; set; } = string.Empty;

    [DataField]
    public TimeSpan Length;
}
