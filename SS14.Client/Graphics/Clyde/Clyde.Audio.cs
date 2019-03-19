using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using OpenTK;
using OpenTK.Audio.OpenAL;
using SS14.Shared.Log;

namespace SS14.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private IntPtr _openALDevice;
        private ContextHandle _openALContext;

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
    }
}
