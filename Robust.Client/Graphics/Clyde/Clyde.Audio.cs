using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly Dictionary<int, WeakReference<AudioSource>> _audioSources = new Dictionary<int, WeakReference<AudioSource>>();

        private readonly HashSet<string> _alcExtensions = new HashSet<string>();
        private readonly HashSet<string> _alContextExtensions = new HashSet<string>();

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
                _openALContext = Alc.CreateContext(_openALDevice, (int*)0);
            }
            Alc.MakeContextCurrent(_openALContext);
            _checkAlcError(_openALDevice);
            _checkAlError();

            // Load up AL context extensions.
            foreach (var extension in AL.Get(ALGetString.Extensions).Split(' '))
            {
                _alContextExtensions.Add(extension);
            }

            Logger.DebugS("oal", "OpenAL Vendor: {0}", AL.Get(ALGetString.Vendor));
            Logger.DebugS("oal", "OpenAL Renderer: {0}", AL.Get(ALGetString.Renderer));
            Logger.DebugS("oal", "OpenAL Version: {0}", AL.Get(ALGetString.Version));
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
                    Logger.WarningS("oal", "Unable to open preferred audio device '{0}': {1}. Falling back default.",
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
        }

        public IClydeAudioSource CreateAudioSource(AudioStream stream)
        {
            var source = AL.GenSource();
            // ReSharper disable once PossibleInvalidOperationException
            AL.Source(source, ALSourcei.Buffer, _audioSampleBuffers[stream.ClydeHandle.Value.ClydeHandle].BufferHandle);
            var audioSource = new AudioSource(this, source);
            _audioSources.Add(source, new WeakReference<AudioSource>(audioSource));
            return audioSource;
        }

        private static void _checkAlcError(IntPtr device,
            [CallerMemberName] string callerMember = "",
            [CallerLineNumber] int callerLineNumber = -1)
        {
            var error = Alc.GetError(device);
            if (error != AlcError.NoError)
            {
                Logger.ErrorS("oal", "[{0}:{1}] ALC error: {2}", callerMember, callerLineNumber, error);
            }
        }

        private static void _checkAlError([CallerMemberName] string callerMember = "",
            [CallerLineNumber] int callerLineNumber = -1)
        {
            var error = AL.GetError();
            if (error != ALError.NoError)
            {
                Logger.ErrorS("oal", "[{0}:{1}] AL error: {2}", callerMember, callerLineNumber, error);
            }
        }

        public AudioStream LoadAudioOggVorbis(Stream stream)
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
                    AL.BufferData(buffer, format, (IntPtr)ptr, vorbis.Data.Length * sizeof(float), (int)vorbis.SampleRate);
                }
            }

            _checkAlError();

            var handle = new Handle(_audioSampleBuffers.Count);
            _audioSampleBuffers.Add(new LoadedAudioSample(buffer));
            var length = TimeSpan.FromSeconds(vorbis.TotalSamples / (double) vorbis.SampleRate);
            return new AudioStream(handle, length);
        }

        public AudioStream LoadAudioWav(Stream stream)
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
                    AL.BufferData(buffer, format, (IntPtr)ptr, wav.Data.Length, wav.SampleRate);
                }
            }
            _checkAlError();

            var handle = new Handle(_audioSampleBuffers.Count);
            _audioSampleBuffers.Add(new LoadedAudioSample(buffer));
            var length = TimeSpan.FromSeconds(wav.Data.Length / (double)wav.BlockAlign / wav.SampleRate);
            return new AudioStream(handle, length);
        }

        private sealed class LoadedAudioSample
        {
            public readonly int BufferHandle;

            public LoadedAudioSample(int bufferHandle)
            {
                BufferHandle = bufferHandle;
            }
        }

        private sealed class AudioSource : IClydeAudioSource
        {
            public Clyde Master;
            public int SourceHandle;

            public AudioSource(Clyde master, int sourceHandle)
            {
                Master = master;
                SourceHandle = sourceHandle;
            }

            public void StartPlaying()
            {
                _checkDisposed();
                AL.SourcePlay(SourceHandle);
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

            public void SetGlobal()
            {
                _checkDisposed();
                AL.Source(SourceHandle, ALSourceb.SourceRelative, true);
                _checkAlError();
            }

            public void SetVolume(float decibels)
            {
                _checkDisposed();
                AL.Source(SourceHandle, ALSourcef.Gain, (float)Math.Pow(10, decibels/10));
                _checkAlError();
            }

            public void SetPosition(Vector2 position)
            {
                _checkDisposed();
                var (x, y) = position;
                AL.Source(SourceHandle, ALSource3f.Position, x, y, 0);
                _checkAlError();
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
                AL.DeleteSource(SourceHandle);
                Master._audioSources.Remove(SourceHandle);
                _checkAlError();
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
    }
}
