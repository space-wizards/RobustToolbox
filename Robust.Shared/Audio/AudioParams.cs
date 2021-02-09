using Robust.Shared.Serialization;
using System;
using System.Diagnostics.Contracts;
using Robust.Shared.Interfaces.Serialization;

namespace Robust.Shared.Audio
{
    /// <summary>
    ///     Contains common audio parameters for audio playback on the client.
    /// </summary>
    [Serializable, NetSerializable]
    public struct AudioParams : IExposeData
    {
        /// <summary>
        ///     Base volume to play the audio at, in dB.
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        ///     Scale for the audio pitch.
        /// </summary>
        public float PitchScale { get; set; }

        /// <summary>
        ///     Audio bus to play on.
        /// </summary>
        public string BusName { get; set; }

        /// <summary>
        ///     Only applies to positional audio.
        ///     The maximum distance from which the audio is hearable.
        /// </summary>
        public float MaxDistance { get; set; }

        /// <summary>
        ///     Only applies to positional audio.
        ///     Positional audio is dampened over distance with this as exponent.
        /// </summary>
        public float Attenuation { get; set; }

        /// <summary>
        ///     Only applies to global (non-positional) audio.
        ///     Target channels if the audio configuration has more than 2 speakers.
        /// </summary>
        public AudioMixTarget MixTarget { get; set; }

        public bool Loop { get; set; }

        public float PlayOffsetSeconds { get; set; }

        // For the max distance value: it's 2000 in Godot, but I assume that's PIXELS due to the 2D positioning,
        // so that's divided by 32 (EyeManager.PIXELSPERMETER).
        /// <summary>
        ///     The "default" audio configuration.
        /// </summary>
        public static readonly AudioParams Default = new(0, 1, "Master", 62.5f, 1, AudioMixTarget.Stereo, false, 0f);

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            Volume = serializer.ReadDataField("volume", 0f);
            PitchScale = serializer.ReadDataField("pitchscale", 1f);
            BusName = serializer.ReadDataField("busname", "Master");
            MaxDistance = serializer.ReadDataField("maxdistance", 62.5f);
            Attenuation = serializer.ReadDataField("attenuation", 1f);
            MixTarget = serializer.ReadDataField("mixtarget", AudioMixTarget.Stereo);
            Loop = serializer.ReadDataField("loop", false);
            PlayOffsetSeconds = serializer.ReadDataField("playoffset", 0f);
        }

        public AudioParams(float volume, float pitchScale, string busName, float maxDistance, float attenuation,
            AudioMixTarget mixTarget, bool loop, float playOffsetSeconds) : this()
        {
            Volume = volume;
            PitchScale = pitchScale;
            BusName = busName;
            MaxDistance = maxDistance;
            Attenuation = attenuation;
            MixTarget = mixTarget;
            Loop = loop;
            PlayOffsetSeconds = playOffsetSeconds;
        }

        /// <summary>
        ///     Returns a copy of this instance with a new volume set, for easy chaining.
        /// </summary>
        /// <param name="volume">The new volume.</param>
        [Pure]
        public AudioParams WithVolume(float volume)
        {
            var me = this;
            me.Volume = volume;
            return me;
        }

        /// <summary>
        ///     Returns a copy of this instance with a new pitch scale set, for easy chaining.
        /// </summary>
        /// <param name="pitch">The new pitch scale.</param>
        [Pure]
        public AudioParams WithPitchScale(float pitch)
        {
            var me = this;
            me.PitchScale = pitch;
            return me;
        }

        /// <summary>
        ///     Returns a copy of this instance with a new bus name set, for easy chaining.
        /// </summary>
        /// <param name="bus">The new bus name.</param>
        [Pure]
        public AudioParams WithBusName(string bus)
        {
            var me = this;
            me.BusName = bus;
            return me;
        }

        /// <summary>
        ///     Returns a copy of this instance with a new attenuation set, for easy chaining.
        /// </summary>
        /// <param name="attenuation">The new attenuation.</param>
        [Pure]
        public AudioParams WithAttenuation(float attenuation)
        {
            var me = this;
            me.Attenuation = attenuation;
            return me;
        }

        /// <summary>
        ///     Returns a copy of this instance with a new max distance set, for easy chaining.
        /// </summary>
        /// <param name="dist">The new max distance.</param>
        [Pure]
        public AudioParams WithMaxDistance(float dist)
        {
            var me = this;
            me.MaxDistance = dist;
            return me;
        }
        /// <summary>
        ///     Returns a copy of this instance with a new mix target set, for easy chaining.
        /// </summary>
        /// <param name="mixTarget">The new mix target.</param>
        [Pure]
        public AudioParams WithMixTarget(AudioMixTarget mixTarget)
        {
            var me = this;
            me.MixTarget = mixTarget;
            return me;
        }

        /// <summary>
        ///     Returns a copy of this instance with a loop set, for easy chaining.
        /// </summary>
        /// <param name="loop">The new loop.</param>
        [Pure]
        public AudioParams WithLoop(bool loop)
        {
            var me = this;
            me.Loop = loop;
            return me;
        }

        [Pure]
        public AudioParams WithPlayOffset(float offset)
        {
            var me = this;
            me.PlayOffsetSeconds = offset;
            return me;
        }
    }

    /// <summary>
    ///     Controls target channels for non-positional audio if the audio configuration has more than 2 speakers.
    /// </summary>
    public enum AudioMixTarget : byte
    {
        // These match the values in the Godot enum,
        // but this is shared so we can't reference it.
        /// <summary>
        ///     The audio will only be played on the first channel.
        /// </summary>
        Stereo = 0,

        /// <summary>
        ///     The audio will be played on all surround channels.
        /// </summary>
        Surround = 1,

        /// <summary>
        ///     The audio will be played on the second channel, which is usually the center.
        /// </summary>
        Center = 2,
    }
}
