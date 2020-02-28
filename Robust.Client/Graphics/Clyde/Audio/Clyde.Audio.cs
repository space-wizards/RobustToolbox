using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using OpenTK;
using OpenTK.Audio.OpenAL;
using Robust.Client.Audio;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Log;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private IntPtr _openALDevice;
        private ContextHandle _openALContext;

        private readonly List<LoadedAudioSample> _audioSampleBuffers = new List<LoadedAudioSample>();

        private readonly Dictionary<int, WeakReference<AudioSource>> _audioSources =
            new Dictionary<int, WeakReference<AudioSource>>();

        private readonly Dictionary<uint, WeakReference<BufferedAudioSource>> _bufferedAudioSources =
            new Dictionary<uint, WeakReference<BufferedAudioSource>>();

        private readonly HashSet<string> _alcExtensions = new HashSet<string>();
        private readonly HashSet<string> _alContextExtensions = new HashSet<string>();

        // Used to track audio sources that were disposed in the finalizer thread,
        // so we need to properly send them off in the main thread.
        private readonly ConcurrentQueue<int> _sourceDisposeQueue = new ConcurrentQueue<int>();
        private readonly ConcurrentQueue<uint> _bufferedSourceDisposeQueue = new ConcurrentQueue<uint>();
        private readonly ConcurrentQueue<uint> _bufferDisposeQueue = new ConcurrentQueue<uint>();

        private bool _alcHasExtension(string extension) => _alcExtensions.Contains(extension);
        private bool _alContentHasExtension(string extension) => _alContextExtensions.Contains(extension);

        private void _initializeAudio()
        {
            _audioOpenDevice();

            // Create OpenAL context.
            _audioCreateContext();
        }

        private void _audioCreateContext()
        {
            unsafe
            {
                _openALContext = Alc.CreateContext(_openALDevice, (int*) 0);
            }

            Alc.MakeContextCurrent(_openALContext);
            _checkAlcError(_openALDevice);
            _checkAlError();

            // Load up AL context extensions.
            foreach (var extension in AL.Get(ALGetString.Extensions).Split(' '))
            {
                _alContextExtensions.Add(extension);
            }

            Logger.DebugS("clyde.oal", "OpenAL Vendor: {0}", AL.Get(ALGetString.Vendor));
            Logger.DebugS("clyde.oal", "OpenAL Renderer: {0}", AL.Get(ALGetString.Renderer));
            Logger.DebugS("clyde.oal", "OpenAL Version: {0}", AL.Get(ALGetString.Version));
        }

        private void _audioOpenDevice()
        {
            // Load up ALC extensions.
            foreach (var extension in Alc.GetString(IntPtr.Zero, AlcGetString.Extensions).Split(' '))
            {
                _alcExtensions.Add(extension);
            }

            var preferredDevice = _configurationManager.GetCVar<string>("audio.device");

            // Open device.
            if (!string.IsNullOrEmpty(preferredDevice))
            {
                _openALDevice = Alc.OpenDevice(preferredDevice);
                if (_openALDevice == IntPtr.Zero)
                {
                    Logger.WarningS("clyde.oal", "Unable to open preferred audio device '{0}': {1}. Falling back default.",
                        preferredDevice, Alc.GetError(IntPtr.Zero));

                    _openALDevice = Alc.OpenDevice(null);
                }
            }
            else
            {
                _openALDevice = Alc.OpenDevice(null);
            }

            _checkAlcError(_openALDevice);

            if (_openALDevice == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Unable to open OpenAL device! {Alc.GetError(IntPtr.Zero)}");
            }
        }

        private void _shutdownAudio()
        {
            foreach (var source in _audioSources.Values.ToArray())
            {
                if (source.TryGetTarget(out var target))
                {
                    target.Dispose();
                }
            }

            foreach (var source in _bufferedAudioSources.Values.ToArray())
            {
                if (source.TryGetTarget(out var target))
                {
                    target.Dispose();
                }
            }

            if (_openALContext != ContextHandle.Zero)
            {
                Alc.DestroyContext(_openALContext);
            }

            if (_openALDevice != IntPtr.Zero)
            {
                Alc.CloseDevice(_openALDevice);
            }
        }

        private void _updateAudio()
        {
            var eye = _eyeManager.CurrentEye;
            var (x, y) = eye.Position.Position;
            AL.Listener(ALListener3f.Position, x, y, -5);

            // Clear out finalized audio sources.
            while (_sourceDisposeQueue.TryDequeue(out var handle))
            {
                Logger.DebugS("clyde.oal", "Cleaning out source {0} which finalized in another thread.", handle);
                AL.DeleteSource(handle);
                _checkAlError();
                _audioSources.Remove(handle);
            }

            // Clear out finalized buffered audio sources.
            while (_bufferedSourceDisposeQueue.TryDequeue(out var handle))
            {
                Logger.DebugS("clyde.oal", "Cleaning out buffered source {0} which finalized in another thread.", handle);
                AL.DeleteSource((int) handle);
                _checkAlError();
                _bufferedAudioSources.Remove(handle);
            }

            // Clear out finalized audio buffers.
            while (_bufferDisposeQueue.TryDequeue(out var handle))
            {
                AL.DeleteBuffer((int) handle);
                _checkAlError();
            }
        }

        public IClydeAudioSource CreateAudioSource(AudioStream stream)
        {
            var source = AL.GenSource();
            // ReSharper disable once PossibleInvalidOperationException
            // TODO: This really shouldn't be indexing based on the ClydeHandle...
            AL.Source(source, ALSourcei.Buffer, _audioSampleBuffers[(int) stream.ClydeHandle.Value.Value].BufferHandle);
            var audioSource = new AudioSource(this, source, stream);
            _audioSources.Add(source, new WeakReference<AudioSource>(audioSource));
            return audioSource;
        }

        public IClydeBufferedAudioSource CreateBufferedAudioSource(int buffers)
        {
            var source = (uint) AL.GenSource();
            // ReSharper disable once PossibleInvalidOperationException
            var bufferHandles = AL.GenBuffers(buffers);
            var unsignedBufferHandles = new uint[bufferHandles.Length];

            for (var i = 0; i < bufferHandles.Length; i++)
                unsignedBufferHandles[i] = (uint) bufferHandles[i];

            var audioSource = new BufferedAudioSource(this, source, unsignedBufferHandles);
            _bufferedAudioSources.Add(source, new WeakReference<BufferedAudioSource>(audioSource));
            return audioSource;
        }

        private static void _checkAlcError(IntPtr device,
            [CallerMemberName] string callerMember = "",
            [CallerLineNumber] int callerLineNumber = -1)
        {
            var error = Alc.GetError(device);
            if (error != AlcError.NoError)
            {
                Logger.ErrorS("clyde.oal", "[{0}:{1}] ALC error: {2}", callerMember, callerLineNumber, error);
            }
        }

        private static void _checkAlError([CallerMemberName] string callerMember = "",
            [CallerLineNumber] int callerLineNumber = -1)
        {
            var error = AL.GetError();
            if (error != ALError.NoError)
            {
                Logger.ErrorS("clyde.oal", "[{0}:{1}] AL error: {2}", callerMember, callerLineNumber, error);
            }
        }

        public AudioStream LoadAudioOggVorbis(Stream stream, string name = null)
        {
            var vorbis = _readOggVorbis(stream);

            var buffer = AL.GenBuffer();

            ALFormat format;
            // NVorbis only supports loading into floats.
            // If this becomes a problem due to missing extension support (doubt it but ok),
            // check the git history, I originally used libvorbisfile which worked and loaded 16 bit LPCM.
            if (vorbis.Channels == 1)
            {
                format = ALFormat.MonoFloat32Ext;
            }
            else if (vorbis.Channels == 2)
            {
                format = ALFormat.StereoFloat32Ext;
            }
            else
            {
                throw new InvalidOperationException("Unable to load audio with more than 2 channels.");
            }

            unsafe
            {
                fixed (float* ptr = vorbis.Data.Span)
                {
                    AL.BufferData(buffer, format, (IntPtr) ptr, vorbis.Data.Length * sizeof(float),
                        (int) vorbis.SampleRate);
                }
            }

            _checkAlError();

            var handle = new ClydeHandle(_audioSampleBuffers.Count);
            _audioSampleBuffers.Add(new LoadedAudioSample(buffer));
            var length = TimeSpan.FromSeconds(vorbis.TotalSamples / (double) vorbis.SampleRate);
            return new AudioStream(handle, length, (int) vorbis.Channels, name);
        }

        public AudioStream LoadAudioWav(Stream stream, string name = null)
        {
            var wav = _readWav(stream);

            var buffer = AL.GenBuffer();

            ALFormat format;
            if (wav.BitsPerSample == 16)
            {
                if (wav.NumChannels == 1)
                {
                    format = ALFormat.Mono16;
                }
                else if (wav.NumChannels == 2)
                {
                    format = ALFormat.Stereo16;
                }
                else
                {
                    throw new InvalidOperationException("Unable to load audio with more than 2 channels.");
                }
            }
            else if (wav.BitsPerSample == 8)
            {
                if (wav.NumChannels == 1)
                {
                    format = ALFormat.Mono8;
                }
                else if (wav.NumChannels == 2)
                {
                    format = ALFormat.Stereo8;
                }
                else
                {
                    throw new InvalidOperationException("Unable to load audio with more than 2 channels.");
                }
            }
            else
            {
                throw new InvalidOperationException("Unable to load wav with bits per sample different from 8 or 16");
            }

            unsafe
            {
                fixed (byte* ptr = wav.Data.Span)
                {
                    AL.BufferData(buffer, format, (IntPtr) ptr, wav.Data.Length, wav.SampleRate);
                }
            }

            _checkAlError();

            var handle = new ClydeHandle(_audioSampleBuffers.Count);
            _audioSampleBuffers.Add(new LoadedAudioSample(buffer));
            var length = TimeSpan.FromSeconds(wav.Data.Length / (double) wav.BlockAlign / wav.SampleRate);
            return new AudioStream(handle, length, wav.NumChannels, name);
        }

        private sealed class LoadedAudioSample
        {
            public readonly int BufferHandle;

            public LoadedAudioSample(int bufferHandle)
            {
                BufferHandle = bufferHandle;
            }
        }

        private void DeleteSourceOnMainThread(int sourceHandle)
        {
            _sourceDisposeQueue.Enqueue(sourceHandle);
        }

        private void DeleteBufferedSourceOnMainThread(uint bufferedSourceHandle)
        {
            _bufferedSourceDisposeQueue.Enqueue(bufferedSourceHandle);
        }

        private void DeleteAudioBufferOnMainThread(uint bufferHandle)
        {
            _bufferDisposeQueue.Enqueue(bufferHandle);
        }

        private sealed class AudioSource : IClydeAudioSource
        {
            private int SourceHandle;
            private readonly Clyde _master;
            private readonly AudioStream _sourceStream;
#if DEBUG
            private bool _didPositionWarning;
#endif

            public AudioSource(Clyde master, int sourceHandle, AudioStream sourceStream)
            {
                _master = master;
                SourceHandle = sourceHandle;
                _sourceStream = sourceStream;
            }

            public void StartPlaying()
            {
                _checkDisposed();
                AL.SourcePlay(SourceHandle);
                _checkAlError();
            }

            public void StopPlaying()
            {
                _checkDisposed();
                AL.SourceStop(SourceHandle);
                _checkAlError();
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
                    _checkAlError();
                    return ret;
                }
                set
                {
                    _checkDisposed();
                    AL.Source(SourceHandle, ALSourceb.Looping, value);
                    _checkAlError();
                }
            }

            public void SetGlobal()
            {
                _checkDisposed();
                AL.Source(SourceHandle, ALSourceb.SourceRelative, true);
                _checkAlError();
            }

            public void SetVolume(float decibels)
            {
                _checkDisposed();
                AL.Source(SourceHandle, ALSourcef.Gain, (float) Math.Pow(10, decibels / 10));
                _checkAlError();
            }

            public void SetPlaybackPosition(float seconds)
            {
                _checkDisposed();
                AL.Source(SourceHandle, ALSourcef.SecOffset, seconds);
                _checkAlError();
            }

            public bool SetPosition(Vector2 position)
            {
                _checkDisposed();

                var (x, y) = position;

                if (!ValidatePosition(x, y))
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
                _checkAlError();
                return true;
            }

            private static bool ValidatePosition(float x, float y)
            {
                if (float.IsFinite(x) && float.IsFinite(y))
                {
                    return true;
                }

                return false;
            }

            public void SetPitch(float pitch)
            {
                _checkDisposed();
                AL.Source(SourceHandle, ALSourcef.Pitch, pitch);
                _checkAlError();
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
                    _master.DeleteSourceOnMainThread(SourceHandle);
                }
                else
                {
                    AL.DeleteSource(SourceHandle);
                    _master._audioSources.Remove(SourceHandle);
                    _checkAlError();
                }

                SourceHandle = -1;
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
            private uint? SourceHandle = null;
            private uint[] BufferHandles;
            private Dictionary<uint, uint> BufferMap = new Dictionary<uint, uint>();
            private readonly Clyde _master;
            private bool _mono = true;

            public int SampleRate { get; set; } = 44100;

            public BufferedAudioSource(Clyde master, uint sourceHandle, uint[] bufferHandles)
            {
                _master = master;
                SourceHandle = sourceHandle;
                BufferHandles = bufferHandles;
                for (uint i = 0; i < BufferHandles.Length; i++)
                {
                    var bufferHandle = BufferHandles[i];
                    BufferMap[bufferHandle] = i;
                }
            }

            public void StartPlaying()
            {
                _checkDisposed();
                // ReSharper disable once PossibleInvalidOperationException
                AL.SourcePlay(SourceHandle.Value);
                _checkAlError();
            }

            public void StopPlaying()
            {
                _checkDisposed();
                // ReSharper disable once PossibleInvalidOperationException
                AL.SourceStop(SourceHandle.Value);
                _checkAlError();
            }

            public bool IsPlaying
            {
                get
                {
                    _checkDisposed();
                    // ReSharper disable once PossibleInvalidOperationException
                    var state = AL.GetSourceState(SourceHandle.Value);
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
                AL.Source(SourceHandle.Value, ALSourceb.SourceRelative, true);
                _checkAlError();
            }

            public void SetLooping()
            {
                // TODO?waaaaddDDDDD
            }

            public void SetVolume(float decibels)
            {
                _checkDisposed();
                // ReSharper disable once PossibleInvalidOperationException
                AL.Source(SourceHandle.Value, ALSourcef.Gain, (float) Math.Pow(10, decibels / 10));
                _checkAlError();
            }

            public void SetPlaybackPosition(float seconds)
            {
                _checkDisposed();
                // ReSharper disable once PossibleInvalidOperationException
                AL.Source(SourceHandle.Value, ALSourcef.SecOffset, seconds);
                _checkAlError();
            }

            public bool SetPosition(Vector2 position)
            {
                _checkDisposed();

                var (x, y) = position;

                if (!ValidatePosition(x, y))
                {
                    return false;
                }

                _mono = true;
                // ReSharper disable once PossibleInvalidOperationException
                AL.Source(SourceHandle.Value, ALSource3f.Position, x, y, 0);
                _checkAlError();
                return true;
            }

            private static bool ValidatePosition(float x, float y)
            {
                if (float.IsFinite(x) && float.IsFinite(y))
                {
                    return true;
                }

                return false;
            }

            public void SetPitch(float pitch)
            {
                _checkDisposed();
                // ReSharper disable once PossibleInvalidOperationException
                AL.Source(SourceHandle.Value, ALSourcef.Pitch, pitch);
                _checkAlError();
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
                if (!disposing)
                {
                    // We can't run this code inside the finalizer thread so tell Clyde to clear it up later.
                    _master.DeleteBufferedSourceOnMainThread(SourceHandle.Value);
                    for (var i = 0; i < BufferHandles.Length; i++)
                        _master.DeleteAudioBufferOnMainThread(BufferHandles[i]);
                }
                else
                {
                    AL.DeleteSource((int) SourceHandle.Value);
                    AL.DeleteBuffers(BufferHandles);
                    _master._bufferedAudioSources.Remove(SourceHandle.Value);
                    _checkAlError();
                }

                SourceHandle = null;
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
                AL.GetSource(SourceHandle.Value, ALGetSourcei.BuffersProcessed, out var buffersProcessed);
                return buffersProcessed;
            }

            public unsafe void GetBuffersProcessed(Span<uint> handles)
            {
                _checkDisposed();
                var entries = Math.Min(Math.Min(handles.Length, BufferHandles.Length), GetNumberOfBuffersProcessed());
                fixed (uint* ptr = handles)
                    // ReSharper disable once PossibleInvalidOperationException
                    AL.SourceUnqueueBuffers(SourceHandle.Value, entries, ptr);

                for (var i = 0; i < entries; i++)
                    handles[i] = BufferMap[handles[i]];
            }

            public unsafe void WriteBuffer(uint handle, ReadOnlySpan<ushort> data)
            {
                _checkDisposed();

                if (handle >= BufferHandles.Length)
                    throw new ArgumentOutOfRangeException(nameof(handle),
                        $"Got {handle}. Expected less than {BufferHandles.Length}");

                fixed (ushort* ptr = data)
                {
                    AL.BufferData(BufferHandles[handle], _mono ? ALFormat.Mono16 : ALFormat.Stereo16, (IntPtr) ptr,
                        _mono ? data.Length / 2 * sizeof(ushort) : data.Length * sizeof(ushort), SampleRate);
                }
            }

            public unsafe void QueueBuffers(ReadOnlySpan<uint> handles)
            {
                _checkDisposed();

                Span<uint> realHandles = stackalloc uint[handles.Length];
                handles.CopyTo(realHandles);

                for (var i = 0; i < realHandles.Length; i++)
                {
                    var handle = realHandles[i];
                    if (handle >= BufferHandles.Length)
                        throw new ArgumentOutOfRangeException(nameof(handles), $"Invalid handle with index {i}!");
                    realHandles[i] = BufferHandles[handle];
                }

                fixed (uint* ptr = realHandles)
                    // ReSharper disable once PossibleInvalidOperationException
                    AL.SourceQueueBuffers(SourceHandle.Value, handles.Length, ptr);
            }

            public unsafe void EmptyBuffers()
            {
                _checkDisposed();
                var length = (SampleRate / BufferHandles.Length) * (_mono ? 1 : 2);

                var empty = new ushort[length];
                var span = (Span<ushort>) empty;
                Span<uint> handles = stackalloc uint[BufferHandles.Length];
                for (var i = 0; i < BufferHandles.Length; i++)
                {
                    WriteBuffer(BufferMap[BufferHandles[i]], span);
                    handles[i] = BufferMap[BufferHandles[i]];
                }

                QueueBuffers(handles);
            }
        }
    }
}
