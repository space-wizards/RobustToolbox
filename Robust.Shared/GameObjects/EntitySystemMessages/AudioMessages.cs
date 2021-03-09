using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using System;

#nullable disable

namespace Robust.Shared.GameObjects
{
    // TODO: This is quite bandwidth intensive.
    // Sending bus names and file names as strings is expensive and can be optimized.
    // Also there's redundant fields in AudioParams in most cases.
    [Serializable, NetSerializable]
    public abstract class AudioMessage : EntityEventArgs
    {
        public uint Identifier { get; set; }
        public string FileName { get; set; }
        public AudioParams AudioParams { get; set; }
    }

    [Serializable, NetSerializable]
    public class StopAudioMessageClient : EntityEventArgs
    {
        public uint Identifier {get; set;}
    }

    [Serializable, NetSerializable]
    public class PlayAudioGlobalMessage : AudioMessage
    {
    }

    [Serializable, NetSerializable]
    public class PlayAudioPositionalMessage : AudioMessage
    {
        public EntityCoordinates Coordinates { get; set; }
    }

    [Serializable, NetSerializable]
    public class PlayAudioEntityMessage : AudioMessage
    {
        public EntityCoordinates Coordinates { get; set; }
        public EntityUid EntityUid { get; set; }
    }
}
