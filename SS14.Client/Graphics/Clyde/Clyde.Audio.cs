using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using OpenTK;
using OpenTK.Audio.OpenAL;
using SS14.Client.Audio;
using SS14.Client.Interfaces.Graphics;
using SS14.Shared.Log;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private IntPtr _openALDevice;
        private ContextHandle _openALContext;

        private readonly List<LoadedAudioSample> _audioSampleBuffers = new List<LoadedAudioSample>();

        private readonly HashSet<string> _alcExtensions = new HashSet<string>();
        private readonly HashSet<string> _alContextExtensions = new HashSet<string>();

        private bool _alcHasExtension(string extension) => _alcExtensions.Contains(extension);
        private bool _alContentHasExtension(string extension) => _alContextExtensions.Contains(extension);

        private void _initializeAudio()
        {
            _audioOpenDevice();

            // Create OpenAL context.
            _audioCreateContext();

            //var orientation = new OpenTK.Vector3(0, 0, 1);
            //var orientationUp = new OpenTK.Vector3(0, 1, 0);
            //AL.Listener(ALListenerfv.Orientation, ref orientation, ref orientationUp);
        }

        private void _audioCreateContext()
        {
            _openALContext = Alc.CreateContext(_openALDevice, Array.Empty<int>());
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
            if (preferredDevice != null)
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
            AL.Source(source, ALSourcei.Buffer, _audioSampleBuffers[stream.ClydeHandle.Value.ClydeHandle].BufferHandle);
            return new AudioSource(source);
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
            if (vorbis.Channels == 1)
            {
                format = ALFormat.Mono16;
            }
            else if (vorbis.Channels == 2)
            {
                format = ALFormat.Stereo16;
            }
            else
            {
                throw new InvalidOperationException("Unable to load audio with more than 2 channels.");
            }

            unsafe
            {
                fixed (byte* ptr = vorbis.Data.Span)
                {
                    AL.BufferData(buffer, format, (IntPtr)ptr, vorbis.Data.Length, (int)vorbis.SampleRate);
                }
            }

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

            var handle = new Handle(_audioSampleBuffers.Count);
            _audioSampleBuffers.Add(new LoadedAudioSample(buffer));
            var length = TimeSpan.FromSeconds(wav.Data.Length / (double)wav.BlockAlign / wav.SampleRate);
            return new AudioStream(handle, length);
        }

        private sealed class LoadedAudioSample
        {
            public int BufferHandle;

            public LoadedAudioSample(int bufferHandle)
            {
                BufferHandle = bufferHandle;
            }
        }

        private sealed class AudioSource : IClydeAudioSource
        {
            public int SourceHandle;

            public AudioSource(int sourceHandle)
            {
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
