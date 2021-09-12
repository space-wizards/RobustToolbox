using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using OpenToolkit.Audio.OpenAL;
using OpenToolkit.Audio.OpenAL.Extensions.Creative.EFX;
using Robust.Client.Audio;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Log;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private ALDevice _openALDevice;
        private ALContext _openALContext;

        private readonly List<LoadedAudioSample> _audioSampleBuffers = new();

        private readonly Dictionary<int, WeakReference<AudioSource>> _audioSources =
            new();

        private readonly Dictionary<int, WeakReference<BufferedAudioSource>> _bufferedAudioSources =
            new();

        private readonly HashSet<string> _alcDeviceExtensions = new();
        private readonly HashSet<string> _alContextExtensions = new();

        // Used to track audio sources that were disposed in the finalizer thread,
        // so we need to properly send them off in the main thread.
        private readonly ConcurrentQueue<(int sourceHandle, int filterHandle)> _sourceDisposeQueue = new();
        private readonly ConcurrentQueue<(int sourceHandle, int filterHandle)> _bufferedSourceDisposeQueue = new();
        private readonly ConcurrentQueue<int> _bufferDisposeQueue = new();

        // The base gain value for a listener, used to boost the default volume.
        private const float _baseGain = 2f;

        public bool HasAlDeviceExtension(string extension) => _alcDeviceExtensions.Contains(extension);
        public bool HasAlContextExtension(string extension) => _alContextExtensions.Contains(extension);

        internal bool IsEfxSupported;

        private ISawmill _openALSawmill = default!;

        private void _initializeAudio()
        {
            _openALSawmill = Logger.GetSawmill("clyde.oal");

            _audioOpenDevice();

            // Create OpenAL context.
            _audioCreateContext();

            IsEfxSupported = HasAlDeviceExtension("ALC_EXT_EFX");

            _cfg.OnValueChanged(CVars.AudioMasterVolume, SetMasterVolume, true);
            _cfg.OnValueChanged(CVars.AudioAttenuation, SetAudioAttenuation, true);
        }

        private void _audioCreateContext()
        {
            unsafe
            {
                _openALContext = ALC.CreateContext(_openALDevice, (int*) 0);
            }

            ALC.MakeContextCurrent(_openALContext);
            _checkAlcError(_openALDevice);
            _checkAlError();

            // Load up AL context extensions.
            var s = ALC.GetString(ALDevice.Null, AlcGetString.Extensions) ?? "";
            foreach (var extension in s.Split(' '))
            {
                _alContextExtensions.Add(extension);
            }

            _openALSawmill.Debug("OpenAL Vendor: {0}", AL.Get(ALGetString.Vendor));
            _openALSawmill.Debug("OpenAL Renderer: {0}", AL.Get(ALGetString.Renderer));
            _openALSawmill.Debug("OpenAL Version: {0}", AL.Get(ALGetString.Version));
        }

        private void _audioOpenDevice()
        {
            var preferredDevice = _cfg.GetCVar(CVars.AudioDevice);

            // Open device.
            if (!string.IsNullOrEmpty(preferredDevice))
            {
                _openALDevice = ALC.OpenDevice(preferredDevice);
                if (_openALDevice == IntPtr.Zero)
                {
                    _openALSawmill.Warning("Unable to open preferred audio device '{0}': {1}. Falling back default.",
                        preferredDevice, ALC.GetError(ALDevice.Null));

                    _openALDevice = ALC.OpenDevice(null);
                }
            }
            else
            {
                _openALDevice = ALC.OpenDevice(null);
            }

            _checkAlcError(_openALDevice);

            if (_openALDevice == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Unable to open OpenAL device! {ALC.GetError(ALDevice.Null)}");
            }

            // Load up ALC extensions.
            var s = ALC.GetString(_openALDevice, AlcGetString.Extensions) ?? "";
            foreach (var extension in s.Split(' '))
            {
                _alcDeviceExtensions.Add(extension);
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

            if (_openALContext != ALContext.Null)
            {
                ALC.DestroyContext(_openALContext);
            }

            if (_openALDevice != IntPtr.Zero)
            {
                ALC.CloseDevice(_openALDevice);
            }
        }

        private void _updateAudio()
        {
            var eye = _eyeManager.CurrentEye;
            var (x, y) = eye.Position.Position;
            AL.Listener(ALListener3f.Position, x, y, -5);

            // Clear out finalized audio sources.
            while (_sourceDisposeQueue.TryDequeue(out var handles))
            {
                _openALSawmill.Debug("Cleaning out source {0} which finalized in another thread.", handles.sourceHandle);
                if (IsEfxSupported) RemoveEfx(handles);
                AL.DeleteSource(handles.sourceHandle);
                _checkAlError();
                _audioSources.Remove(handles.sourceHandle);
            }

            // Clear out finalized buffered audio sources.
            while (_bufferedSourceDisposeQueue.TryDequeue(out var handles))
            {
                _openALSawmill.Debug("Cleaning out buffered source {0} which finalized in another thread.", handles.sourceHandle);
                if (IsEfxSupported) RemoveEfx(handles);
                AL.DeleteSource(handles.sourceHandle);
                _checkAlError();
                _bufferedAudioSources.Remove(handles.sourceHandle);
            }

            // Clear out finalized audio buffers.
            while (_bufferDisposeQueue.TryDequeue(out var handle))
            {
                AL.DeleteBuffer((int) handle);
                _checkAlError();
            }
        }

        private static void RemoveEfx((int sourceHandle, int filterHandle) handles)
        {
            if (handles.filterHandle != 0) EFX.DeleteFilter(handles.filterHandle);
        }

        public void SetMasterVolume(float newVolume)
        {
            AL.Listener(ALListenerf.Gain, _baseGain * newVolume);
        }

        public void SetAudioAttenuation(int value)
        {
            var attenuation = (Attenuation) value;

            switch (attenuation)
            {
                case Attenuation.NoAttenuation:
                    AL.DistanceModel(ALDistanceModel.None);
                    break;
                case Attenuation.InverseDistance:
                    AL.DistanceModel(ALDistanceModel.InverseDistance);
                    break;
                case Attenuation.Default:
                case Attenuation.InverseDistanceClamped:
                    AL.DistanceModel(ALDistanceModel.InverseDistanceClamped);
                    break;
                case Attenuation.LinearDistance:
                    AL.DistanceModel(ALDistanceModel.LinearDistance);
                    break;
                case Attenuation.LinearDistanceClamped:
                    AL.DistanceModel(ALDistanceModel.LinearDistanceClamped);
                    break;
                case Attenuation.ExponentDistance:
                    AL.DistanceModel(ALDistanceModel.ExponentDistance);
                    break;
                case Attenuation.ExponentDistanceClamped:
                    AL.DistanceModel(ALDistanceModel.ExponentDistanceClamped);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"No implementation to set {attenuation.ToString()} for DistanceModel!");
            }

            var attToString = attenuation == Attenuation.Default ? Attenuation.InverseDistanceClamped : attenuation;

            _openALSawmill.Info($"Set audio attenuation to {attToString.ToString()}");
        }

        public IClydeAudioSource CreateAudioSource(AudioStream stream)
        {
            var source = AL.GenSource();
            // ReSharper disable once PossibleInvalidOperationException
            // TODO: This really shouldn't be indexing based on the ClydeHandle...
            AL.Source(source, ALSourcei.Buffer, _audioSampleBuffers[(int) stream.ClydeHandle!.Value.Value].BufferHandle);

            var audioSource = new AudioSource(this, source, stream);
            _audioSources.Add(source, new WeakReference<AudioSource>(audioSource));
            return audioSource;
        }

        public IClydeBufferedAudioSource CreateBufferedAudioSource(int buffers, bool floatAudio=false)
        {
            var source = AL.GenSource();

            // ReSharper disable once PossibleInvalidOperationException

            var audioSource = new BufferedAudioSource(this, source, AL.GenBuffers(buffers), floatAudio);
            _bufferedAudioSources.Add(source, new WeakReference<BufferedAudioSource>(audioSource));
            return audioSource;
        }

        private void _checkAlcError(ALDevice device,
            [CallerMemberName] string callerMember = "",
            [CallerLineNumber] int callerLineNumber = -1)
        {
            var error = ALC.GetError(device);
            if (error != AlcError.NoError)
            {
                _openALSawmill.Error("[{0}:{1}] ALC error: {2}", callerMember, callerLineNumber, error);
            }
        }

        private void _checkAlError([CallerMemberName] string callerMember = "",
            [CallerLineNumber] int callerLineNumber = -1)
        {
            var error = AL.GetError();
            if (error != ALError.NoError)
            {
                _openALSawmill.Error("[{0}:{1}] AL error: {2}", callerMember, callerLineNumber, error);
            }
        }

        public AudioStream LoadAudioOggVorbis(Stream stream, string? name = null)
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

        public AudioStream LoadAudioWav(Stream stream, string? name = null)
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

        public AudioStream LoadAudioRaw(ReadOnlySpan<short> samples, int channels, int sampleRate, string? name = null)
        {
            var fmt = channels switch
            {
                1 => ALFormat.Mono16,
                2 => ALFormat.Stereo16,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(channels), "Only stereo and mono is currently supported")
            };

            var buffer = AL.GenBuffer();
            _checkAlError();

            unsafe
            {
                fixed (short* ptr = samples)
                {
                    AL.BufferData(buffer, fmt, (IntPtr) ptr, samples.Length * sizeof(short), sampleRate);
                }
            }

            _checkAlError();

            var handle = new ClydeHandle(_audioSampleBuffers.Count);
            var length = TimeSpan.FromSeconds((double) samples.Length / channels / sampleRate);
            _audioSampleBuffers.Add(new LoadedAudioSample(buffer));
            return new AudioStream(handle, length, channels, name);
        }

        private sealed class LoadedAudioSample
        {
            public readonly int BufferHandle;

            public LoadedAudioSample(int bufferHandle)
            {
                BufferHandle = bufferHandle;
            }
        }

        private void DeleteSourceOnMainThread(int sourceHandle, int filterHandle)
        {
            _sourceDisposeQueue.Enqueue((sourceHandle, filterHandle));
        }

        private void DeleteBufferedSourceOnMainThread(int bufferedSourceHandle, int filterHandle)
        {
            _bufferedSourceDisposeQueue.Enqueue((bufferedSourceHandle, filterHandle));
        }

        private void DeleteAudioBufferOnMainThread(int bufferHandle)
        {
            _bufferDisposeQueue.Enqueue(bufferHandle);
        }

        private sealed class AudioSource : IClydeAudioSource
        {
            private int SourceHandle;
            private readonly Clyde _master;
            private readonly AudioStream _sourceStream;
            private int FilterHandle;
#if DEBUG
            private bool _didPositionWarning;
#endif

            private float _gain;

            private bool IsEfxSupported => _master.IsEfxSupported;

            public AudioSource(Clyde master, int sourceHandle, AudioStream sourceStream)
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
            private readonly Clyde _master;
            private bool _mono = true;
            private bool _float = false;
            private int FilterHandle;

            private float _gain;

            public int SampleRate { get; set; } = 44100;

            private bool IsEfxSupported => _master.IsEfxSupported;

            public BufferedAudioSource(Clyde master, int sourceHandle, int[] bufferHandles, bool floatAudio = false)
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

                if (!disposing || Thread.CurrentThread != _master._gameThread)
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
