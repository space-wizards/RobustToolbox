using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.Input;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Robust.Client.Graphics.Audio
{
    /// <summary>
    ///     Hey look, it's ClydeAudio.AudioSource's evil twin brother!
    /// </summary>
    internal class DummyAudioSource : IClydeAudioSource
    {
        public static DummyAudioSource Instance { get; } = new();

        public bool IsPlaying => default;
        public bool IsLooping { get; set; }

        public void Dispose()
        {
            // Nada.
        }

        public void StartPlaying()
        {
            // Nada.
        }

        public void StopPlaying()
        {
            // Nada.
        }

        public bool SetPosition(Vector2 position)
        {
            return true;
        }

        public void SetPitch(float pitch)
        {
            // Nada.
        }

        public void SetGlobal()
        {
            // Nada.
        }

        public void SetVolume(float decibels)
        {
            // Nada.
        }

        public void SetVolumeDirect(float scale)
        {
            // Nada.
        }

        public void SetMaxDistance(float maxDistance)
        {
            // Nada.
        }

        public void SetRolloffFactor(float rolloffFactor)
        {
            // Nada.
        }

        public void SetReferenceDistance(float refDistance)
        {
            // Nada.
        }

        public void SetOcclusion(float blocks)
        {
            // Nada.
        }

        public void SetPlaybackPosition(float seconds)
        {
            // Nada.
        }

        public void SetVelocity(Vector2 velocity)
        {
            // Nada.
        }
    }
}
