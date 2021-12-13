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
    ///     For "start ss14 with no audio devices" Smugleaf
    /// </summary>
    [UsedImplicitly]
    internal abstract class ProxyClydeAudio : IClydeAudio, IClydeAudioInternal
    {
        protected IClydeAudioInternal ActualImplementation = default!;

        public virtual bool InitializePostWindowing()
        {
            // This particular implementation exists to be overridden because removing this method causes C# to complain
            return ActualImplementation.InitializePostWindowing();
        }

        public void FrameProcess(FrameEventArgs eventArgs)
        {
            ActualImplementation.FrameProcess(eventArgs);
        }

        public void Shutdown()
        {
            ActualImplementation.Shutdown();
        }

        public AudioStream LoadAudioOggVorbis(Stream stream, string? name = null)
        {
            return ActualImplementation.LoadAudioOggVorbis(stream, name);
        }

        public AudioStream LoadAudioWav(Stream stream, string? name = null)
        {
            return ActualImplementation.LoadAudioWav(stream, name);
        }

        public AudioStream LoadAudioRaw(ReadOnlySpan<short> samples, int channels, int sampleRate, string? name = null)
        {
            return ActualImplementation.LoadAudioRaw(samples, channels, sampleRate, name);
        }

        public IClydeAudioSource CreateAudioSource(AudioStream stream)
        {
            return ActualImplementation.CreateAudioSource(stream);
        }

        public IClydeBufferedAudioSource CreateBufferedAudioSource(int buffers, bool floatAudio = false)
        {
            return ActualImplementation.CreateBufferedAudioSource(buffers, floatAudio);
        }

        public void SetMasterVolume(float newVolume)
        {
            ActualImplementation.SetMasterVolume(newVolume);
        }
    }
}
