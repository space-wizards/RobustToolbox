using System;

namespace Robust.Client.Interfaces.Graphics
{
    public interface IClydeBufferedAudioSource : IClydeAudioSource
    {
        int SampleRate { get; set; }
        int GetNumberOfBuffersProcessed();
        void GetBuffersProcessed(Span<uint> handles);
        void WriteBuffer(uint handle, ReadOnlySpan<ushort> data);
        void QueueBuffers(ReadOnlySpan<uint> handles);
        void EmptyBuffers();
    }
}