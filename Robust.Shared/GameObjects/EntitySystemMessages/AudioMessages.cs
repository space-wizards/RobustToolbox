using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using System;

namespace Robust.Shared.GameObjects
{
    // TODO: This is quite bandwidth intensive.
    // Sending bus names and file names as strings is expensive and can be optimized.
    // Also there's redundant fields in AudioParams in most cases.
    [Serializable, NetSerializable]
    public abstract class AudioMessage : EntitySystemMessage
    {
        public ushort Identifier { get; set; }
        public string FileName { get; set; }
        public AudioParams AudioParams { get; set; }
    }

    [Serializable, NetSerializable]
    public class StopAudioMessageClient : AudioMessage
    {

    }

    /// <summary>
    /// DO NOT USE.
    /// This is actually used for removing an identifier from the Server's list of stream ids.
    /// </summary>
    [Serializable, NetSerializable]
    internal class StopAudioMessageServer: AudioMessage
    {

    }

    [Serializable, NetSerializable]
    public class PlayAudioGlobalMessage : AudioMessage
    {
    }

    [Serializable, NetSerializable]
    public class PlayAudioPositionalMessage : AudioMessage
    {
        public GridCoordinates Coordinates { get; set; }
    }

    [Serializable, NetSerializable]
    public class PlayAudioEntityMessage : AudioMessage
    {
        public GridCoordinates Coordinates { get; set; }
        public EntityUid EntityUid { get; set; }
    }
}
