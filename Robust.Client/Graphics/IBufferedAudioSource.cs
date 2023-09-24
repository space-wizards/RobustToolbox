using System;
using Robust.Client.Audio.Sources;
using Robust.Shared.Audio.Sources;

namespace Robust.Client.Graphics;

internal interface IBufferedAudioSource : IAudioSource
{
    int SampleRate { get; set; }
    int GetNumberOfBuffersProcessed();
    void GetBuffersProcessed(Span<int> handles);
    void WriteBuffer(int handle, ReadOnlySpan<ushort> data);
    void WriteBuffer(int handle, ReadOnlySpan<float> data);
    void QueueBuffers(ReadOnlySpan<int> handles);
    void EmptyBuffers();
}
