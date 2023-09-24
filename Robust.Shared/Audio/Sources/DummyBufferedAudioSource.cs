using System;

namespace Robust.Shared.Audio.Sources
{
    /// <summary>
    ///     Hey look, it's Audio.BufferedAudioSource's evil twin brother!
    /// </summary>
    internal sealed class DummyBufferedAudioSource : DummyAudioSource, IBufferedAudioSource
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
