using Robust.Shared.Serialization;
using System;
using System.Diagnostics.Contracts;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Audio
{
    public enum Attenuation : int
    {
        // https://hackage.haskell.org/package/OpenAL-1.7.0.5/docs/Sound-OpenAL-AL-Attenuation.html

        Invalid = 0,
        NoAttenuation = 1 << 0,
        InverseDistance = 1 << 1,
        InverseDistanceClamped = 1 << 2,
        LinearDistance = 1 << 3,
        LinearDistanceClamped = 1 << 4,
        ExponentDistance = 1 << 5,
        ExponentDistanceClamped = 1 << 6,
    }

    /// <summary>
    ///     Contains common audio parameters for audio playback on the client.
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public partial struct AudioParams
    {
        private float _volume = Default.Volume;

        /// <summary>
        ///     Base volume to play the audio at, in dB.
        /// </summary>
        [DataField]
        public float Volume
        {
            get => _volume;
            set
            {
                if (float.IsNaN(value))
                {
                    value = float.NegativeInfinity;
                }

                _volume = value;
            }
        }

        /// <summary>
        ///     Scale for the audio pitch.
        /// </summary>
        [DataField]
        public float Pitch
        {
            get => _pitch;
            set => _pitch = MathF.Max(0f, value);
        }

        private float _pitch = Default.Pitch;

        /// <summary>
        ///     Only applies to positional audio.
        ///     The maximum distance from which the audio is hearable.
        /// </summary>
        [DataField]
        public float MaxDistance { get; set; } = Default.MaxDistance;

        /// <summary>
        ///     Used for distance attenuation calculations. Set to 0f to make a sound exempt from distance attenuation.
        /// </summary>
        [DataField]
        public float RolloffFactor { get; set; } = Default.RolloffFactor;

        /// <summary>
        ///     Equivalent of the minimum distance to use for an audio source.
        /// </summary>
        [DataField]
        public float ReferenceDistance { get; set; } = Default.ReferenceDistance;

        [DataField]
        public bool Loop { get; set; } = Default.Loop;

        [DataField]
        public float PlayOffsetSeconds { get; set; } = Default.PlayOffsetSeconds;

        /// <summary>
        ///     If not null, this will randomly modify the pitch scale by adding a number drawn from a normal distribution with this deviation.
        /// </summary>
        [DataField]
        public float? Variation { get; set; } = null;

        // For the max distance value: it's 2000 in Godot, but I assume that's PIXELS due to the 2D positioning,
        // so that's divided by 32 (EyeManager.PIXELSPERMETER).
        /// <summary>
        ///     The "default" audio configuration.
        /// </summary>
        public static readonly AudioParams Default = new(0, 1, SharedAudioSystem.DefaultSoundRange, 1, 1, false, 0f);

        // explicit parameterless constructor required so that default values get set properly.
        public AudioParams() { }

        public AudioParams(
            float volume,
            float pitch,
            float maxDistance,
            float refDistance,
            bool loop,
            float playOffsetSeconds,
            float? variation = null)
            : this(volume, pitch, maxDistance, 1, refDistance, loop, playOffsetSeconds, variation)
        {
        }

        public AudioParams(float volume, float pitch, float maxDistance,float rolloffFactor, float refDistance, bool loop, float playOffsetSeconds, float? variation = null) : this()
        {
            Volume = volume;
            Pitch = pitch;
            MaxDistance = maxDistance;
            RolloffFactor = rolloffFactor;
            ReferenceDistance = refDistance;
            Loop = loop;
            PlayOffsetSeconds = playOffsetSeconds;
            Variation = variation;
        }

        /// <summary>
        ///     Returns a copy of this instance with a new volume set, for easy chaining.
        /// </summary>
        /// <param name="volume">The new volume.</param>
        [Pure]
        public readonly AudioParams WithVolume(float volume)
        {
            var me = this;
            me.Volume = volume;
            return me;
        }

        /// <summary>
        ///     Returns a copy of this instance with a modified volume set, for easy chaining.
        /// </summary>
        /// <param name="volume">The volume to add.</param>
        [Pure]
        public readonly AudioParams AddVolume(float volume)
        {
            var me = this;
            me.Volume += volume;
            return me;
        }

        /// <summary>
        ///     Returns a copy of this instance with a new variation set, for easy chaining.
        /// </summary>
        [Pure]
        public readonly AudioParams WithVariation(float? variation)
        {
            var me = this;
            me.Variation = variation;
            return me;
        }

        /// <summary>
        ///     Returns a copy of this instance with a new pitch scale set, for easy chaining.
        /// </summary>
        /// <param name="pitch">The new pitch scale.</param>
        [Pure]
        public readonly AudioParams WithPitchScale(float pitch)
        {
            var me = this;
            me.Pitch = pitch;
            return me;
        }

        /// <summary>
        ///     Returns a copy of this instance with a new max distance set, for easy chaining.
        /// </summary>
        /// <param name="dist">The new max distance.</param>
        [Pure]
        public readonly AudioParams WithMaxDistance(float dist)
        {
            var me = this;
            me.MaxDistance = dist;
            return me;
        }

        /// <summary>
        ///     Returns a copy of this instance with a new rolloff factor set, for easy chaining.
        /// </summary>
        /// <param name="rolloffFactor">The new rolloff factor.</param>
        [Pure]
        public readonly AudioParams WithRolloffFactor(float rolloffFactor)
        {
            var me = this;
            me.RolloffFactor = rolloffFactor;
            return me;
        }

        /// <summary>
        ///     Returns a copy of this instance with a new reference distance set, for easy chaining.
        /// </summary>
        /// <param name="refDistance">The new reference distance.</param>
        [Pure]
        public readonly AudioParams WithReferenceDistance(float refDistance)
        {
            var me = this;
            me.ReferenceDistance = refDistance;
            return me;
        }

        /// <summary>
        ///     Returns a copy of this instance with a loop set, for easy chaining.
        /// </summary>
        /// <param name="loop">The new loop.</param>
        [Pure]
        public readonly AudioParams WithLoop(bool loop)
        {
            var me = this;
            me.Loop = loop;
            return me;
        }

        [Pure]
        public readonly AudioParams WithPlayOffset(float offset)
        {
            var me = this;
            me.PlayOffsetSeconds = offset;
            return me;
        }
    }
}
