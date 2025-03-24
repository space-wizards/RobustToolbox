using System;
using System.Collections.Generic;
using OpenTK.Audio.OpenAL;
using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;
using Robust.Shared.Audio.Sources;

namespace Robust.Client.Audio.Sources;

internal sealed class BufferedAudioSource : BaseAudioSource, IBufferedAudioSource
{
    private int[] BufferHandles;
    private Dictionary<int, int> BufferMap = new();
    private readonly AudioManager _master;
    private bool _mono = true;
    private bool _float = false;

    public int SampleRate { get; set; } = 44100;

    private bool IsEfxSupported => _master.IsEfxSupported;

    public BufferedAudioSource(AudioManager master, int sourceHandle, int[] bufferHandles, bool floatAudio = false) : base(master, sourceHandle)
    {
        _master = master;
        SourceHandle = sourceHandle;
        BufferHandles = bufferHandles;
        for (int i = 0; i < BufferHandles.Length; i++)
        {
            var bufferHandle = BufferHandles[i];
            BufferMap[bufferHandle] = i;
        }
        _float = floatAudio;
    }

    /// <inheritdoc />
    public override bool Playing
    {
        get
        {
            _checkDisposed();
            var state = AL.GetSourceState(SourceHandle);
            _master._checkAlError();
            return state == ALSourceState.Playing;
        }
        set
        {
            if (value)
            {
                _checkDisposed();
                // IDK why this stackallocs but gonna leave it for now.
                AL.SourcePlay(stackalloc int[] {SourceHandle});
                _master._checkAlError();
            }
            else
            {
                if (_isDisposed())
                    return;

                AL.SourceStop(SourceHandle);
                _master._checkAlError();
            }
        }
    }

    ~BufferedAudioSource()
    {
        Dispose(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (SourceHandle == -1)
            return;

        if (!_master.IsMainThread())
        {
            // We can't run this code inside another thread so tell Clyde to clear it up later.
            _master.DeleteBufferedSourceOnMainThread(SourceHandle, FilterHandle);

            foreach (var handle in BufferHandles)
            {
                _master.DeleteAudioBufferOnMainThread(handle);
            }
        }
        else
        {
            if (FilterHandle != 0)
                EFX.DeleteFilter(FilterHandle);

            AL.DeleteSource(SourceHandle);
            AL.DeleteBuffers(BufferHandles);
            _master.RemoveBufferedAudioSource(SourceHandle);
            _master._checkAlError();
        }

        FilterHandle = 0;
        SourceHandle = -1;
    }

    public int GetNumberOfBuffersProcessed()
    {
        _checkDisposed();
        // ReSharper disable once PossibleInvalidOperationException
        AL.GetSource(SourceHandle, ALGetSourcei.BuffersProcessed, out var buffersProcessed);
        return buffersProcessed;
    }

    public unsafe void GetBuffersProcessed(Span<int> handles)
    {
        _checkDisposed();
        var entries = Math.Min(Math.Min(handles.Length, BufferHandles.Length), GetNumberOfBuffersProcessed());
        fixed (int* ptr = handles)
        {
            AL.SourceUnqueueBuffers(SourceHandle, entries, ptr);
        }

        for (var i = 0; i < entries; i++)
        {
            handles[i] = BufferMap[handles[i]];
        }
    }

    public unsafe void WriteBuffer(int handle, ReadOnlySpan<ushort> data)
    {
        _checkDisposed();

        if(_float)
            throw new InvalidOperationException("Can't write ushort numbers to buffers when buffer type is float!");

        if (handle >= BufferHandles.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(handle),
                $"Got {handle}. Expected less than {BufferHandles.Length}");
        }

        fixed (ushort* ptr = data)
        {
            AL.BufferData(BufferHandles[handle], _mono ? ALFormat.Mono16 : ALFormat.Stereo16, (IntPtr) ptr,
                _mono ? data.Length / 2 * sizeof(ushort) : data.Length * sizeof(ushort), SampleRate);
        }
    }

    public unsafe void WriteBuffer(int handle, ReadOnlySpan<float> data)
    {
        _checkDisposed();

        if(!_float)
            throw new InvalidOperationException("Can't write float numbers to buffers when buffer type is ushort!");

        if (handle >= BufferHandles.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(handle),
                $"Got {handle}. Expected less than {BufferHandles.Length}");
        }

        fixed (float* ptr = data)
        {
            AL.BufferData(BufferHandles[handle], _mono ? ALFormat.MonoFloat32Ext : ALFormat.StereoFloat32Ext, (IntPtr) ptr,
                _mono ? data.Length / 2 * sizeof(float) : data.Length * sizeof(float), SampleRate);
        }
    }

    public unsafe void QueueBuffers(ReadOnlySpan<int> handles)
    {
        _checkDisposed();

        Span<int> realHandles = stackalloc int[handles.Length];
        handles.CopyTo(realHandles);

        for (var i = 0; i < realHandles.Length; i++)
        {
            var handle = realHandles[i];
            if (handle >= BufferHandles.Length)
                throw new ArgumentOutOfRangeException(nameof(handles), $"Invalid handle with index {i}!");
            realHandles[i] = BufferHandles[handle];
        }

        fixed (int* ptr = realHandles)
            // ReSharper disable once PossibleInvalidOperationException
        {
            AL.SourceQueueBuffers(SourceHandle, handles.Length, ptr);
        }
    }

    public unsafe void EmptyBuffers()
    {
        _checkDisposed();
        var length = SampleRate / BufferHandles.Length * (_mono ? 1 : 2);

        Span<int> handles = stackalloc int[BufferHandles.Length];

        if (_float)
        {
            var empty = new float[length];
            var span = (Span<float>) empty;

            for (var i = 0; i < BufferHandles.Length; i++)
            {
                WriteBuffer(BufferMap[BufferHandles[i]], span);
                handles[i] = BufferMap[BufferHandles[i]];
            }
        }
        else
        {
            var empty = new ushort[length];
            var span = (Span<ushort>) empty;

            for (var i = 0; i < BufferHandles.Length; i++)
            {
                WriteBuffer(BufferMap[BufferHandles[i]], span);
                handles[i] = BufferMap[BufferHandles[i]];
            }
        }

        QueueBuffers(handles);
    }
}
