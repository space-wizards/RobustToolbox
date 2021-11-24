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
    ///     Hey look, it's ClydeAudio.BufferedAudioSource's evil twin brother!
    /// </summary>
    internal sealed class DummyBufferedAudioSource : DummyAudioSource, IClydeBufferedAudioSource
    {
        public new static DummyBufferedAudioSource Instance { get; } = new();
        public int SampleRate { get; set; } = 0;

        public void WriteBuffer(int handle, ReadOnlySpan<ushort> data)
        {
            // Nada.
        }

        public void WriteBuffer(int handle, ReadOnlySpan<float> data)
        {
            // Nada.
        }

        public void QueueBuffers(ReadOnlySpan<int> handles)
        {
            // Nada.
        }

        public void EmptyBuffers()
        {
            // Nada.
        }

        public void GetBuffersProcessed(Span<int> handles)
        {
            // Nada.
        }

        public int GetNumberOfBuffersProcessed()
        {
            return 0;
        }
    }
}
