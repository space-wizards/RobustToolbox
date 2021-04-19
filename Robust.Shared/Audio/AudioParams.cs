using Robust.Shared.Serialization;
using System;
using System.Diagnostics.Contracts;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Audio
{
    /// <summary>
    ///     Contains common audio parameters for audio playback on the client.
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public struct AudioParams : IPopulateDefaultValues
    {
        /// <summary>
        ///     Base volume to play the audio at, in dB.
        /// </summary>
        [DataField("volume")]
        public float Volume { get; set; }

        /// <summary>
        ///     Scale for the audio pitch.
        /// </summary>
        [DataField("pitchscale")]
        public float PitchScale { get; set; }

        /// <summary>
        ///     Audio bus to play on.
        /// </summary>
        [DataField("busname")]
        public string BusName { get; set; }

        /// <summary>
        ///     Only applies to positional audio.
        ///     The maximum distance from which the audio is hearable.
        /// </summary>
        [DataField("maxdistance")]
        public float MaxDistance { get; set; }

        /// <summary>
        ///     Only applies to positional audio.
        ///     Positional audio is dampened over distance with this as exponent.
        /// </summary>
        [DataField("attenuation")]
        public float Attenuation { get; set; }

        [DataField("loop")]
        public bool Loop { get; set; }

        [DataField("playoffset")]
        public float PlayOffsetSeconds { get; set; }

        // For the max distance value: it's 2000 in Godot, but I assume that's PIXELS due to the 2D positioning,
        // so that's divided by 32 (EyeManager.PIXELSPERMETER).
        /// <summary>
        ///     The "default" audio configuration.
        /// </summary>
        public static readonly AudioParams Default = new(0, 1, "Master", 62.5f, 1, false, 0f);

        public AudioParams(float volume, float pitchScale, string busName, float maxDistance, float attenuation, bool loop, float playOffsetSeconds) : this()
        {
            Volume = volume;
            PitchScale = pitchScale;
            BusName = busName;
            MaxDistance = maxDistance;
            Attenuation = attenuation;
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

        public void PopulateDefaultValues()
        {
            PitchScale = 1f;
            BusName = "Master";
            MaxDistance = 62.5f;
            Attenuation = 1f;
        }
    }
}
