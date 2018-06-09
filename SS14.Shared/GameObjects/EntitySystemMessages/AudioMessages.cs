using SS14.Shared.Audio;
using SS14.Shared.Map;
using SS14.Shared.Serialization;
using System;

namespace SS14.Shared.GameObjects
{
    // TODO: This is quite bandwidth intensive.
    // Sending bus names and file names as strings is expensive and can be optimized.
    // Also there's redundant fields in AudioParams in most cases.
    [Serializable, NetSerializable]
    public abstract class AudioMessage : EntitySystemMessage
    {
        public string FileName { get; set; }
        public AudioParams AudioParams { get; set; }
    }

    [Serializable, NetSerializable]
    public class PlayAudioGlobalMessage : AudioMessage
    {
    }

    [Serializable, NetSerializable]
    public class PlayAudioPositionalMessage : AudioMessage
    {
        public GridLocalCoordinates Coordinates { get; set; }
    }

    [Serializable, NetSerializable]
    public class PlayAudioEntityMessage : AudioMessage
    {
        public GridLocalCoordinates Coordinates { get; set; }
        public EntityUid EntityUid { get; set; }
    }
}
