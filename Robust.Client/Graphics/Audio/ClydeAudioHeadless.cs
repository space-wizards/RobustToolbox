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
    ///     Hey look, it's ClydeAudio's evil twin brother!
    /// </summary>
    [UsedImplicitly]
    internal sealed class ClydeAudioHeadless : IClydeAudio, IClydeAudioInternal
    {
        public bool InitializePostWindowing()
        {
            return true;
        }

        public void FrameProcess(FrameEventArgs eventArgs)
        {
        }

        public void Shutdown()
        {
        }

        public AudioStream LoadAudioOggVorbis(Stream stream, string? name = null)
        {
            // TODO: Might wanna actually load this so the length gets reported correctly.
            return new(default, default, 1, name);
        }

        public AudioStream LoadAudioWav(Stream stream, string? name = null)
        {
            // TODO: Might wanna actually load this so the length gets reported correctly.
            return new(default, default, 1, name);
        }

        public AudioStream LoadAudioRaw(ReadOnlySpan<short> samples, int channels, int sampleRate, string? name = null)
        {
            // TODO: Might wanna actually load this so the length gets reported correctly.
            return new(default, default, channels, name);
        }

        public IClydeAudioSource CreateAudioSource(AudioStream stream)
        {
            return DummyAudioSource.Instance;
        }

        public IClydeBufferedAudioSource CreateBufferedAudioSource(int buffers, bool floatAudio = false)
        {
            return DummyBufferedAudioSource.Instance;
        }

        public void SetMasterVolume(float newVolume)
        {
            // Nada.
        }
    }
}
