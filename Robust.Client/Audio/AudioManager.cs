using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using OpenTK.Audio.OpenAL;
using Robust.Client.Audio.Sources;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Client.Audio;

internal sealed partial class AudioManager : IAudioInternal
{
    [Shared.IoC.Dependency] private readonly IConfigurationManager _cfg = default!;
    [Shared.IoC.Dependency] private readonly ILogManager _logMan = default!;
    [Shared.IoC.Dependency] private readonly IReloadManager _reload = default!;
    [Shared.IoC.Dependency] private readonly IResourceCache _cache = default!;

    private Thread? _gameThread;

    private ALDevice _openALDevice;
    private ALContext _openALContext;

    private readonly Dictionary<int, LoadedAudioSample> _audioSampleBuffers = new();

    private readonly Dictionary<int, WeakReference<BaseAudioSource>> _audioSources =
        new();

    private readonly Dictionary<int, WeakReference<BufferedAudioSource>> _bufferedAudioSources =
        new();

    private readonly HashSet<string> _alcDeviceExtensions = new();
    private readonly HashSet<string> _alContextExtensions = new();
    private Attenuation _attenuation;

    public bool HasAlDeviceExtension(string extension) => _alcDeviceExtensions.Contains(extension);
    public bool HasAlContextExtension(string extension) => _alContextExtensions.Contains(extension);

    internal bool IsEfxSupported;

    internal ISawmill OpenALSawmill = default!;

    private void _audioCreateContext()
    {
        var useHrtf = _cfg.GetCVar(CVars.AudioHrtf);

        // Load up AL context extensions.
        var s = ALC.GetString(_openALDevice, AlcGetString.Extensions) ?? "";
        foreach (var extension in s.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            _alContextExtensions.Add(extension);
        }

        var supportsHrtf = HasAlContextExtension("ALC_SOFT_HRTF");
        var attributeList = new [] { 0 };
        if (useHrtf && supportsHrtf)
        {
            var hrtfCount = ALC.GetInteger(_openALDevice, (AlcGetInteger)AlcSoftGetInteger.NumHrtfSpecifiers);
            OpenALSawmill.Debug("HRTF specifier count: {0}", hrtfCount);

            if (hrtfCount > 0)
            {
                attributeList =
                [
                    (int) AlcSoftGetInteger.Hrtf, 1,
                    // default to the first specifier, im not even really sure in what contexts more than one of these
                    // exists and i dont think its super valuable to expose another cvar for changing the index
                    (int) AlcSoftGetInteger.HrtfId, 0,
                    0
                ];
            }
            else
            {
                OpenALSawmill.Warning("HRTF support enabled but no supported specifiers, HRTF will be disabled");
            }
        }

        _openALContext = ALC.CreateContext(_openALDevice, attributeList);

        ALC.MakeContextCurrent(_openALContext);
        _checkAlcError(_openALDevice);
        _checkAlError();

        var hrtfEnabled = supportsHrtf ? ALC.GetInteger(_openALDevice, (AlcGetInteger)AlcSoftGetInteger.HrtfStatus) : 0;
        OpenALSawmill.Debug("OpenAL Vendor: {0}", AL.Get(ALGetString.Vendor));
        OpenALSawmill.Debug("OpenAL Renderer: {0}", AL.Get(ALGetString.Renderer));
        OpenALSawmill.Debug("OpenAL Version: {0}", AL.Get(ALGetString.Version));
        OpenALSawmill.Debug("HRTF status: {0}", hrtfEnabled == 1 ? "Enabled" : "Disabled");
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
                OpenALSawmill.Warning("Unable to open preferred audio device '{0}': {1}. Falling back default.",
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
            OpenALSawmill.Error("Unable to open OpenAL device! {1}", ALC.GetError(ALDevice.Null));
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

    private void InitializeAudio()
    {
        OpenALSawmill = _logMan.GetSawmill("clyde.oal");

        if (!_audioOpenDevice())
            return;

        // Create OpenAL context.
        _audioCreateContext();

        IsEfxSupported = HasAlDeviceExtension("ALC_EXT_EFX");

        _cfg.OnValueChanged(CVars.AudioMasterVolume, SetMasterGain, true);

        _reload.Register("/Audio", "*.ogg");
        _reload.Register("/Audio", "*.wav");

        _reload.OnChanged += OnReload;
    }

    private void OnReload(ResPath args)
    {
        if (args.Extension != "ogg" &&
            args.Extension != "wav")
        {
            return;
        }

        _cache.ReloadResource<AudioResource>(args);
    }

    internal bool IsMainThread()
    {
        return Thread.CurrentThread == _gameThread;
    }

    private static void RemoveEfx((int sourceHandle, int filterHandle) handles)
    {
        if (handles.filterHandle != 0)
            ALC.EFX.DeleteFilter(handles.filterHandle);
    }

    private void _checkAlcError(ALDevice device,
        [CallerMemberName] string callerMember = "",
        [CallerLineNumber] int callerLineNumber = -1)
    {
        var error = ALC.GetError(device);
        if (error != AlcError.NoError)
        {
            OpenALSawmill.Error("[{0}:{1}] ALC error: {2}", callerMember, callerLineNumber, error);
        }
    }

    internal void LogError(string message)
    {
        OpenALSawmill.Error(message);
    }

    /// <summary>
    /// Like _checkAlError but allows custom data to be passed in as relevant.
    /// </summary>
    internal void LogALError(ALErrorInterpolatedStringHandler message, [CallerMemberName] string callerMember = "", [CallerLineNumber] int callerLineNumber = -1)
    {
        if (message.Error != ALError.NoError)
        {
            OpenALSawmill.Error("[{0}:{1}] AL error: {2}, {3}. Stacktrace is {4}", callerMember, callerLineNumber, message.Error, message.ToStringAndClear(), Environment.StackTrace);
        }
    }

    public void _checkAlError([CallerMemberName] string callerMember = "", [CallerLineNumber] int callerLineNumber = -1)
    {
        var error = AL.GetError();
        if (error != ALError.NoError)
        {
            OpenALSawmill.Error("[{0}:{1}] AL error: {2}", callerMember, callerLineNumber, error);
        }
    }

    /// <summary>
    ///     OpenAL Soft specific enum. OpenTK packages the regular OpenAL ones, but we use OpenAL Soft on most platforms,
    ///     and these are used at the moment for HRTF support specifically.
    /// </summary>
    // https://github.com/kcat/openal-soft/blob/e9c479eb4190101bc51179afae56fc6dd5d26066/include/AL/alext.h#L471
    public enum AlcSoftGetInteger
    {
        Hrtf = 0x1992,
        HrtfStatus = 0x1993,
        NumHrtfSpecifiers = 0x1994,
        HrtfId = 0x1996
    }

    private sealed class LoadedAudioSample
    {
        public readonly int BufferHandle;

        public LoadedAudioSample(int bufferHandle)
        {
            BufferHandle = bufferHandle;
        }
    }

    [InterpolatedStringHandler]
    internal ref struct ALErrorInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _handler;
        public ALError Error;

        public ALErrorInterpolatedStringHandler(int literalLength, int formattedCount, out bool shouldAppend)
        {
            Error = AL.GetError();
            if (Error == ALError.NoError)
            {
                shouldAppend = false;
                _handler = default;
            }
            else
            {
                shouldAppend = true;
                _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
            }
        }

        public string ToStringAndClear() => _handler.ToStringAndClear();
        public override string ToString() => _handler.ToString();
        public void AppendLiteral(string value) => _handler.AppendLiteral(value);
        public void AppendFormatted<T>(T value) => _handler.AppendFormatted(value);
        public void AppendFormatted<T>(T value, string? format) => _handler.AppendFormatted(value, format);
    }
}
