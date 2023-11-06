using System;

namespace Robust.Client.Graphics
{
    public interface IClydeBufferedAudioSource : IClydeAudioSource
    {
        int SampleRate { get; set; }
        int GetNumberOfBuffersProcessed();
        void GetBuffersProcessed(Span<int> handles);
        void WriteBuffer(int handle, ReadOnlySpan<ushort> data);
        void WriteBuffer(int handle, ReadOnlySpan<float> data);
        void QueueBuffers(ReadOnlySpan<int> handles);
        void EmptyBuffers();
    }
}
