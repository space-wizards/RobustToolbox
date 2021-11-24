using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using OpenToolkit.Audio.OpenAL;
using OpenToolkit.Audio.OpenAL.Extensions.Creative.EFX;
using OpenToolkit.Mathematics;
using Robust.Client.Audio;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Audio;
using Robust.Shared.Log;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Robust.Client.Graphics.Audio
{
    internal partial class ClydeAudio
    {
        private sealed class AudioSource : IClydeAudioSource
        {
            private int SourceHandle;
            private readonly ClydeAudio _master;
            private readonly AudioStream _sourceStream;
            private int FilterHandle;
#if DEBUG
            private bool _didPositionWarning;
#endif

            private float _gain;

            private bool IsEfxSupported => _master.IsEfxSupported;

            public AudioSource(ClydeAudio master, int sourceHandle, AudioStream sourceStream)
            {
                _master = master;
                SourceHandle = sourceHandle;
                _sourceStream = sourceStream;
                AL.GetSource(SourceHandle, ALSourcef.Gain, out _gain);
            }

            public void StartPlaying()
            {
                _checkDisposed();
                AL.SourcePlay(SourceHandle);
                _master._checkAlError();
            }

            public void StopPlaying()
            {
                if (_isDisposed()) return;
                AL.SourceStop(SourceHandle);
                _master._checkAlError();
            }

            public bool IsPlaying
            {
                get
                {
                    _checkDisposed();
                    var state = AL.GetSourceState(SourceHandle);
                    return state == ALSourceState.Playing;
                }
            }

            public bool IsLooping
            {
                get
                {
                    _checkDisposed();
                    AL.GetSource(SourceHandle, ALSourceb.Looping, out var ret);
                    _master._checkAlError();
                    return ret;
                }
                set
                {
                    _checkDisposed();
                    AL.Source(SourceHandle, ALSourceb.Looping, value);
                    _master._checkAlError();
                }
            }

            public void SetGlobal()
            {
                _checkDisposed();
                AL.Source(SourceHandle, ALSourceb.SourceRelative, true);
                _master._checkAlError();
            }

            public void SetVolume(float decibels)
            {
                _checkDisposed();
                var priorOcclusion = 1f;
                if (!IsEfxSupported)
                {
                    AL.GetSource(SourceHandle, ALSourcef.Gain, out var priorGain);
                    priorOcclusion = priorGain / _gain;
                }
                _gain =  MathF.Pow(10, decibels / 10);
                AL.Source(SourceHandle, ALSourcef.Gain, _gain * priorOcclusion);
                _master._checkAlError();
            }

            public void SetVolumeDirect(float scale)
            {
                _checkDisposed();
                var priorOcclusion = 1f;
                if (!IsEfxSupported)
                {
                    AL.GetSource(SourceHandle, ALSourcef.Gain, out var priorGain);
                    priorOcclusion = priorGain / _gain;
                }
                _gain = scale;
                AL.Source(SourceHandle, ALSourcef.Gain, _gain * priorOcclusion);
                _master._checkAlError();
            }

            public void SetMaxDistance(float distance)
            {
                _checkDisposed();
                AL.Source(SourceHandle, ALSourcef.MaxDistance, distance);
                _master._checkAlError();
            }

            public void SetRolloffFactor(float rolloffFactor)
            {
                _checkDisposed();
                AL.Source(SourceHandle, ALSourcef.RolloffFactor, rolloffFactor);
                _master._checkAlError();
            }

            public void SetReferenceDistance(float refDistance)
            {
                _checkDisposed();
                AL.Source(SourceHandle, ALSourcef.ReferenceDistance, refDistance);
                _master._checkAlError();
            }

            public void SetOcclusion(float blocks)
            {
                _checkDisposed();
                var cutoff = MathF.Exp(-blocks * 1);
                var gain = MathF.Pow(cutoff, 0.1f);
                if (IsEfxSupported)
                {
                    SetOcclusionEfx(gain, cutoff);
                }
                else
                {
                    gain *= gain * gain;
                    AL.Source(SourceHandle, ALSourcef.Gain, _gain * gain);
                }
                _master._checkAlError();
            }

            private void SetOcclusionEfx(float gain, float cutoff)
            {
                if (FilterHandle == 0)
                {
                    FilterHandle = EFX.GenFilter();
                    EFX.Filter(FilterHandle, FilterInteger.FilterType, (int) FilterType.Lowpass);
                }

                EFX.Filter(FilterHandle, FilterFloat.LowpassGain, gain);
                EFX.Filter(FilterHandle, FilterFloat.LowpassGainHF, cutoff);
                AL.Source(SourceHandle, ALSourcei.EfxDirectFilter, FilterHandle);
            }

            public void SetPlaybackPosition(float seconds)
            {
                _checkDisposed();
                AL.Source(SourceHandle, ALSourcef.SecOffset, seconds);
                _master._checkAlError();
            }

            public bool SetPosition(Vector2 position)
            {
                _checkDisposed();

                var (x, y) = position;

                if (!AreFinite(x, y))
                {
                    return false;
                }
#if DEBUG
                // OpenAL doesn't seem to want to play stereo positionally.
                // Log a warning if people try to.
                if (_sourceStream.ChannelCount > 1 && !_didPositionWarning)
                {
                    _didPositionWarning = true;
                    Logger.WarningS("clyde.oal",
                        "Attempting to set position on audio source with multiple audio channels! Stream: '{0}'",
                        _sourceStream.Name);
                }
#endif

                AL.Source(SourceHandle, ALSource3f.Position, x, y, 0);
                _master._checkAlError();
                return true;
            }

            private static bool AreFinite(float x, float y)
            {
                if (float.IsFinite(x) && float.IsFinite(y))
                {
                    return true;
                }

                return false;
            }

            public void SetVelocity(Vector2 velocity)
            {
                _checkDisposed();

                var (x, y) = velocity;

                if (!AreFinite(x, y))
                {
                    return;
                }

                AL.Source(SourceHandle, ALSource3f.Velocity, x, y, 0);

                _master._checkAlError();
            }

            public void SetPitch(float pitch)
            {
                _checkDisposed();
                AL.Source(SourceHandle, ALSourcef.Pitch, pitch);
                _master._checkAlError();
            }

            ~AudioSource()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (!disposing)
                {
                    // We can't run this code inside the finalizer thread so tell Clyde to clear it up later.
                    _master.DeleteSourceOnMainThread(SourceHandle, FilterHandle);
                }
                else
                {
                    if (FilterHandle != 0) EFX.DeleteFilter(FilterHandle);
                    AL.DeleteSource(SourceHandle);
                    _master._audioSources.Remove(SourceHandle);
                    _master._checkAlError();
                }

                SourceHandle = -1;
            }

            private bool _isDisposed()
            {
                return SourceHandle == -1;
            }

            private void _checkDisposed()
            {
                if (SourceHandle == -1)
                {
                    throw new ObjectDisposedException(nameof(AudioSource));
                }
            }
        }

        private sealed class BufferedAudioSource : IClydeBufferedAudioSource
        {
            private int? SourceHandle = null;
            private int[] BufferHandles;
            private Dictionary<int, int> BufferMap = new();
            private readonly ClydeAudio _master;
            private bool _mono = true;
            private bool _float = false;
            private int FilterHandle;

            private float _gain;

            public int SampleRate { get; set; } = 44100;

            private bool IsEfxSupported => _master.IsEfxSupported;

            public BufferedAudioSource(ClydeAudio master, int sourceHandle, int[] bufferHandles, bool floatAudio = false)
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
                AL.GetSource(sourceHandle, ALSourcef.Gain, out _gain);
            }

            public void StartPlaying()
            {
                _checkDisposed();
                // ReSharper disable once PossibleInvalidOperationException
                AL.SourcePlay(stackalloc int[] {SourceHandle!.Value});
                _master._checkAlError();
            }

            public void StopPlaying()
            {
                if (_isDisposed()) return;
                // ReSharper disable once PossibleInvalidOperationException
                AL.SourceStop(SourceHandle!.Value);
                _master._checkAlError();
            }

            public bool IsPlaying
            {
                get
                {
                    _checkDisposed();
                    // ReSharper disable once PossibleInvalidOperationException
                    var state = AL.GetSourceState(SourceHandle!.Value);
                    return state == ALSourceState.Playing;
                }
            }

            public bool IsLooping
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }

            public void SetGlobal()
            {
                _checkDisposed();
                _mono = false;
                // ReSharper disable once PossibleInvalidOperationException
                AL.Source(SourceHandle!.Value, ALSourceb.SourceRelative, true);
                _master._checkAlError();
            }

            public void SetLooping()
            {
                // TODO?waaaaddDDDDD
            }

            public void SetVolume(float decibels)
            {
                _checkDisposed();
                var priorOcclusion = 1f;
                if (!IsEfxSupported)
                {
                    AL.GetSource(SourceHandle!.Value, ALSourcef.Gain, out var priorGain);
                    priorOcclusion = priorGain / _gain;
                }
                _gain =  MathF.Pow(10, decibels / 10);
                AL.Source(SourceHandle!.Value, ALSourcef.Gain, _gain * priorOcclusion);
                _master._checkAlError();
            }

            public void SetVolumeDirect(float scale)
            {
                _checkDisposed();
                var priorOcclusion = 1f;
                if (!IsEfxSupported)
                {
                    AL.GetSource(SourceHandle!.Value, ALSourcef.Gain, out var priorGain);
                    priorOcclusion = priorGain / _gain;
                }
                _gain = scale;
                AL.Source(SourceHandle!.Value, ALSourcef.Gain, _gain * priorOcclusion);
                _master._checkAlError();
            }

            public void SetMaxDistance(float distance)
            {
                _checkDisposed();
                AL.Source(SourceHandle!.Value, ALSourcef.MaxDistance, distance);
                _master._checkAlError();
            }

            public void SetRolloffFactor(float rolloffFactor)
            {
                _checkDisposed();
                AL.Source(SourceHandle!.Value, ALSourcef.RolloffFactor, rolloffFactor);
                _master._checkAlError();
            }

            public void SetReferenceDistance(float refDistance)
            {
                _checkDisposed();
                AL.Source(SourceHandle!.Value, ALSourcef.ReferenceDistance, refDistance);
                _master._checkAlError();
            }

            public void SetOcclusion(float blocks)
            {
                _checkDisposed();
                var cutoff = MathF.Exp(-blocks * 1.5f);
                var gain = MathF.Pow(cutoff, 0.1f);
                if (IsEfxSupported)
                {
                    SetOcclusionEfx(gain, cutoff);
                }
                else
                {
                    gain *= gain * gain;
                    AL.Source(SourceHandle!.Value, ALSourcef.Gain, gain * _gain);
                }

                _master._checkAlError();
            }

            private void SetOcclusionEfx(float gain, float cutoff)
            {
                if (FilterHandle == 0)
                {
                    FilterHandle = EFX.GenFilter();
                    EFX.Filter(FilterHandle, FilterInteger.FilterType, (int) FilterType.Lowpass);
                }
                EFX.Filter(FilterHandle, FilterFloat.LowpassGain, gain);
                EFX.Filter(FilterHandle, FilterFloat.LowpassGainHF, cutoff);
                AL.Source(SourceHandle!.Value, ALSourcei.EfxDirectFilter, FilterHandle);
            }

            public void SetPlaybackPosition(float seconds)
            {
                _checkDisposed();
                // ReSharper disable once PossibleInvalidOperationException
                AL.Source(SourceHandle!.Value, ALSourcef.SecOffset, seconds);
                _master._checkAlError();
            }

            public bool SetPosition(Vector2 position)
            {
                _checkDisposed();

                var (x, y) = position;

                if (!AreFinite(x, y))
                {
                    return false;
                }

                _mono = true;
                // ReSharper disable once PossibleInvalidOperationException
                AL.Source(SourceHandle!.Value, ALSource3f.Position, x, y, 0);
                _master._checkAlError();
                return true;
            }

            private static bool AreFinite(float x, float y)
            {
                if (float.IsFinite(x) && float.IsFinite(y))
                {
                    return true;
                }

                return false;
            }

            public void SetVelocity(Vector2 velocity)
            {
                _checkDisposed();

                var (x, y) = velocity;

                if (!AreFinite(x, y))
                {
                    return;
                }

                AL.Source(SourceHandle!.Value, ALSource3f.Velocity, x, y, 0);

                _master._checkAlError();
            }

            public void SetPitch(float pitch)
            {
                _checkDisposed();
                // ReSharper disable once PossibleInvalidOperationException
                AL.Source(SourceHandle!.Value, ALSourcef.Pitch, pitch);
                _master._checkAlError();
            }

            ~BufferedAudioSource()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (SourceHandle == null) return;

                if (!_master.IsMainThread())
                {
                    // We can't run this code inside another thread so tell Clyde to clear it up later.
                    _master.DeleteBufferedSourceOnMainThread(SourceHandle.Value, FilterHandle);
                    for (var i = 0; i < BufferHandles.Length; i++)
                        _master.DeleteAudioBufferOnMainThread(BufferHandles[i]);
                }
                else
                {
                    if (FilterHandle != 0) EFX.DeleteFilter(FilterHandle);
                    AL.DeleteSource(SourceHandle.Value);
                    AL.DeleteBuffers(BufferHandles);
                    _master._bufferedAudioSources.Remove(SourceHandle.Value);
                    _master._checkAlError();
                }

                SourceHandle = null;
            }

            private bool _isDisposed()
            {
                return SourceHandle == null;
            }

            private void _checkDisposed()
            {
                if (SourceHandle == null)
                {
                    throw new ObjectDisposedException(nameof(AudioSource));
                }
            }

            public int GetNumberOfBuffersProcessed()
            {
                _checkDisposed();
                // ReSharper disable once PossibleInvalidOperationException
                AL.GetSource(SourceHandle!.Value, ALGetSourcei.BuffersProcessed, out var buffersProcessed);
                return buffersProcessed;
            }

            public unsafe void GetBuffersProcessed(Span<int> handles)
            {
                _checkDisposed();
                var entries = Math.Min(Math.Min(handles.Length, BufferHandles.Length), GetNumberOfBuffersProcessed());
                fixed (int* ptr = handles)
                    // ReSharper disable once PossibleInvalidOperationException
                    AL.SourceUnqueueBuffers(SourceHandle!.Value, entries, ptr);

                for (var i = 0; i < entries; i++)
                    handles[i] = BufferMap[handles[i]];
            }

            public unsafe void WriteBuffer(int handle, ReadOnlySpan<ushort> data)
            {
                _checkDisposed();

                if(_float)
                    throw new InvalidOperationException("Can't write ushort numbers to buffers when buffer type is float!");

                if (handle >= BufferHandles.Length)
                    throw new ArgumentOutOfRangeException(nameof(handle),
                        $"Got {handle}. Expected less than {BufferHandles.Length}");

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
                    throw new ArgumentOutOfRangeException(nameof(handle),
                        $"Got {handle}. Expected less than {BufferHandles.Length}");

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
                    AL.SourceQueueBuffers(SourceHandle!.Value, handles.Length, ptr);
            }

            public unsafe void EmptyBuffers()
            {
                _checkDisposed();
                var length = (SampleRate / BufferHandles.Length) * (_mono ? 1 : 2);

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
    }
}
