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
    internal partial class ClydeAudio : IClydeAudio, IClydeAudioInternal
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

        // The base gain value for a listener, used to boost the default volume.
        private const float _baseGain = 2f;

        public bool HasAlDeviceExtension(string extension) => _alcDeviceExtensions.Contains(extension);
        public bool HasAlContextExtension(string extension) => _alContextExtensions.Contains(extension);

        internal bool IsEfxSupported;

        private ISawmill _openALSawmill = default!;

        private bool _initializeAudio()
        {
            _openALSawmill = Logger.GetSawmill("clyde.oal");

            if (!_audioOpenDevice())
                return false;

            // Create OpenAL context.
            _audioCreateContext();

            IsEfxSupported = HasAlDeviceExtension("ALC_EXT_EFX");

            _cfg.OnValueChanged(CVars.AudioMasterVolume, SetMasterVolume, true);
            _cfg.OnValueChanged(CVars.AudioAttenuation, SetAudioAttenuation, true);
            return true;
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

        private bool _audioOpenDevice()
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
                _openALSawmill.Error("Unable to open OpenAL device! {1}", ALC.GetError(ALDevice.Null));
                return false;
            }

            // Load up ALC extensions.
            var s = ALC.GetString(_openALDevice, AlcGetString.Extensions) ?? "";
            foreach (var extension in s.Split(' '))
            {
                _alcDeviceExtensions.Add(extension);
            }
            return true;
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
            var rot2d = eye.Rotation.ToVec();
            AL.Listener(ALListenerfv.Orientation, new []{0, 0, -1, rot2d.X, rot2d.Y, 0});

            // Default orientation: at: (0, 0, -1)  up: (0, 1, 0)
            var (rotX, rotY) = eye.Rotation.ToVec();
            var at = new Vector3(0f, 0f, -1f);
            var up = new Vector3(rotY, rotX, 0f);
            AL.Listener(ALListenerfv.Orientation, ref at, ref up);

            _flushALDisposeQueues();
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
    }
}
