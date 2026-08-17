using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Robust.Client.Audio.Sources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Robust.Client.Audio;

internal sealed partial class AudioManager : IAudioInternal
{

    [Shared.IoC.Dependency] private IConfigurationManager _cfg = default!;
    [Shared.IoC.Dependency] private ILogManager _logMan = default!;
    [Shared.IoC.Dependency] private IReloadManager _reload = default!;
    [Shared.IoC.Dependency] private IResourceCache _cache = default!;
    [Shared.IoC.Dependency] private IClydeInternal _clyde = default!;
    [Shared.IoC.Dependency] private IGameTiming _gameTiming = default!;

    private const string NullDeviceName = "No Output";
    private const int AlcConnected = 0x313;
    private static readonly TimeSpan DeviceCheckInterval = TimeSpan.FromSeconds(2);

    private bool _audioInitialized;
    private bool _silentFallback;
    private TimeSpan _nextDeviceCheck;
    private int _nextClydeHandle;
    private bool _focused = true;
    private bool _muteUnfocused;
    private const float MasterFadeDuration = 0.25f;
    private float _masterFadeElapsed = MasterFadeDuration;
    private float _masterFadeStartGain = 1f;
    private float _masterFadeTargetGain = 1f;
    private static int _preloaded;

    private Thread? _gameThread;

    private ALDevice _openALDevice;
    private ALContext _openALContext;

    private readonly Dictionary<int, LoadedAudioSample> _audioSampleBuffers = [];

    private readonly Dictionary<int, WeakReference<BaseAudioSource>> _audioSources = [];

    private readonly Dictionary<int, WeakReference<BufferedAudioSource>> _bufferedAudioSources = [];

    private readonly HashSet<string> _alcDeviceExtensions = [];
    private readonly HashSet<string> _alContextExtensions = [];
    private Attenuation _attenuation;

    private ReopenDeviceSoftDelegate? _reopenDevice;

    internal bool IsEfxSupported;
    internal ISawmill OpenALSawmill = default!;

    // ALCboolean is a single byte - do NOT marshal it as C# bool (4-byte BOOL).
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte ReopenDeviceSoftDelegate(ALDevice device, IntPtr deviceName, int[] attribs);

    [LibraryImport("ucrtbase.dll", EntryPoint = "_wputenv_s", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int WPutEnvS(string name, string value);

    private static void ForceEnv(string name, string value)
    {
        Environment.SetEnvironmentVariable(name, value);
        if (!OperatingSystem.IsWindows())
            return;

        try { WPutEnvS(name, value); }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
    }

    private static void PreloadOpenAl(ISawmill sawmill)
    {
        if (Interlocked.Exchange(ref _preloaded, 1) != 0)
            return;

        // Must happen before the library initialises, i.e. before it is loaded.
        // A leading comma appends to the default driver list instead of replacing it,
        // so the null backend only kicks in when every real backend fails.
        ForceEnv("ALSOFT_DRIVERS", ",null");

        if (!OperatingSystem.IsWindows())
            return;

        // Windows dedupes modules by file name, so preloading our copy as OpenAL32.dll
        // makes OpenTK's later LoadLibrary("openal32") resolve to it instead of the
        // Creative router in System32, which reports no devices and no extensions.
        var rid = Environment.Is64BitProcess ? "win-x64" : "win-x86";
        var baseDir = AppContext.BaseDirectory;

        string[] candidates =
        [
            Path.Combine(baseDir, "runtimes", rid, "native", "OpenAL32.dll"),
            Path.Combine(baseDir, "OpenAL32.dll"),
        ];

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
                continue;

            if (NativeLibrary.TryLoad(candidate, out _))
            {
                sawmill.Debug($"Preloaded bundled OpenAL from {candidate}");
                return;
            }

            sawmill.Warning($"Found {candidate} but failed to load it (architecture mismatch or missing dependencies).");
        }

        sawmill.Warning("No bundled OpenAL found, falling back to the system implementation. Hot-plug may be unavailable.");
    }

    #region Initialization

    private void InitializeAudio()
    {
        try
        {
            if (!AudioOpenDevice())
            {
                OpenALSawmill.Warning("Audio will be disabled for this session.");
                return;
            }

            AudioCreateContext();

            if (_openALContext == ALContext.Null)
            {
                OpenALSawmill.Warning("Failed to create an OpenAL context. Audio will be disabled for this session.");
                return;
            }

            LoadReopenExtension();
            _audioInitialized = true;
        }
        catch (Exception e)
        {
            OpenALSawmill.Warning($"Failed to initialize audio, running silent: {e}");
            _audioInitialized = false;
            return;
        }

        _cfg.OnValueChanged(CVars.AudioMasterVolume, SetMasterGain, true);
        _cfg.OnValueChanged(CVars.AudioMuteUnfocused, OnMuteUnfocusedChanged, true);
        _cfg.OnValueChanged(CVars.AudioDevice, OnAudioDeviceChanged);
        _clyde.OnWindowFocused += OnWindowFocused;

        _reload.Register("/Audio", "*.ogg");
        _reload.Register("/Audio", "*.wav");
        _reload.OnChanged += OnReload;
    }

    private void LoadReopenExtension()
    {
        if (!HasAlDeviceExtension("ALC_SOFT_reopen_device"))
        {
            OpenALSawmill.Info("ALC_SOFT_reopen_device is unavailable, audio hot-plug is disabled.");
            return;
        }

        _reopenDevice = LoadAlcDelegate<ReopenDeviceSoftDelegate>("alcReopenDeviceSOFT");

        if (_reopenDevice == null)
            OpenALSawmill.Warning("ALC_SOFT_reopen_device is advertised but alcReopenDeviceSOFT is missing.");
    }

    #endregion

    #region Device Management

    private void OnAudioDeviceChanged(string deviceSpecifier)
    {
        if (!_audioInitialized)
            return;

        SwitchAudioDevice(deviceSpecifier);
    }

    private void SwitchAudioDevice(string requestedDevice)
    {
        OpenALSawmill.Info("Switching OpenAL output device to {0}.",
            string.IsNullOrEmpty(requestedDevice) ? "<default>" : requestedDevice);

        if (TryReopenAudioDevice(requestedDevice))
            return;

        // The skrunkly path that's hopefully never needed.
        OpenALSawmill.Warning("ALC_SOFT_reopen_device is unavailable or failed. Falling back to a full audio device rebuild.");

        DisposeAllAudio();
        FlushALDisposeQueues();

        if (_openALContext != ALContext.Null)
        {
            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(_openALContext);
            _openALContext = ALContext.Null;
        }

        if (_openALDevice != ALDevice.Null)
        {
            ALC.CloseDevice(_openALDevice);
            _openALDevice = ALDevice.Null;
        }

        _audioInitialized = false;

        if (!AudioOpenDevice())
        {
            OpenALSawmill.Error("Failed to reopen OpenAL device after device switch.");
            return;
        }

        AudioCreateContext();
        _audioInitialized = _openALContext != ALContext.Null;
        ApplyDistanceModel();
        ApplyMasterGain();
    }

    private bool AudioOpenDevice()
    {
        var preferred = GetPreferredDeviceName();

        if (preferred != null)
        {
            _openALDevice = ALC.OpenDevice(preferred);
            if (_openALDevice == ALDevice.Null)
                OpenALSawmill.Warning($"Unable to open preferred audio device '{preferred}': " +
                                      $"{ALC.GetError(ALDevice.Null)}. Falling back to the default device.");
        }

        if (_openALDevice == ALDevice.Null)
            _openALDevice = ALC.OpenDevice(null);

        if (_openALDevice == ALDevice.Null)
        {
            // No real output right now. Open the silent backend so the context, buffers
            // and sources stay alive and can be switched to a real device later.
            OpenALSawmill.Info("No audio output available, opening the null device to allow hot-plug later.");
            _openALDevice = ALC.OpenDevice(NullDeviceName);
        }

        if (_openALDevice == ALDevice.Null)
        {
            OpenALSawmill.Error($"Unable to open any OpenAL device: {ALC.GetError(ALDevice.Null)}");
            return false;
        }

        RefreshDeviceState();
        return true;
    }

    private bool TryReopenAudioDevice(string? requestedDevice)
    {
        if (_reopenDevice == null)
            return false;

        var namePtr = requestedDevice == null ? IntPtr.Zero : Marshal.StringToCoTaskMemUTF8(requestedDevice);

        try
        {
            if (_reopenDevice(_openALDevice, namePtr, BuildContextAttributes()) == 0)
            {
                OpenALSawmill.Debug($"alcReopenDeviceSOFT('{requestedDevice ?? "default"}') failed: " +
                                    $"{ALC.GetError(_openALDevice)}");
                return false;
            }
        }
        finally
        {
            if (namePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(namePtr);
        }

        RefreshDeviceState();
        ApplyDistanceModel();
        ApplyMasterGain();
        LogHrtfStatus();
        return true;
    }

    private void RefreshDeviceState()
    {
        _alcDeviceExtensions.Clear();
        var extensions = ALC.GetString(_openALDevice, AlcGetString.Extensions) ?? "";
        foreach (var extension in extensions.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            _alcDeviceExtensions.Add(extension);

        var name = GetCurrentDeviceName();

        // openal-soft prefixes the backend device with "OpenAL Soft on ", so match
        // the suffix rather than comparing the whole string.
        _silentFallback = IsNullDevice(name);

        IsEfxSupported = HasAlDeviceExtension("ALC_EXT_EFX");

        OpenALSawmill.Info($"Audio device: '{name}'{(_silentFallback ? " (silent fallback)" : "")}");
    }

    private string? GetPreferredDeviceName()
    {
        var preferred = _cfg.GetCVar(CVars.AudioDevice);
        return string.IsNullOrEmpty(preferred) ? null : preferred;
    }

    private bool IsDeviceConnected()
    {
        if (!HasAlDeviceExtension("ALC_EXT_disconnect"))
            return true;

        return ALC.GetInteger(_openALDevice, (AlcGetInteger)AlcConnected) != 0;
    }

    private bool HasRealPlaybackDevice()
    {
        foreach (var device in GetAudioDevices())
            if (!string.IsNullOrEmpty(device) && !IsNullDevice(device))
                return true;

        return false;
    }

    #endregion

    #region Master Gain/Fade

    private void OnMuteUnfocusedChanged(bool muteUnfocused)
    {
        _muteUnfocused = muteUnfocused;
        SetMasterFadeTarget(GetMasterFadeTarget());
    }

    private void OnWindowFocused(WindowFocusedEventArgs args)
    {
        if (args.Window != _clyde.MainWindow)
            return;

        _focused = args.Focused;
        SetMasterFadeTarget(GetMasterFadeTarget());
    }

    private float GetMasterFadeTarget() => _muteUnfocused && !_focused ? 0f : 1f;

    private void ApplyMasterGain()
    {
        if (!_audioInitialized) return;

        var effectiveGain = BaseGain * FadeGain;

        #region Platform hack for MacOS
        // HACK/BUG: Apple's OpenAL implementation has a bug where values of 0f for listener gain don't actually
        // HACK/BUG: prevent sound playback. Workaround is to cap the minimum gain at a value just above 0.
        if (OperatingSystem.IsMacOS() && effectiveGain == 0f)
        {
            OpenALSawmill.Verbose("Not setting gain to 0 because Apple can't write an OpenAL implementation");
            AL.Listener(ALListenerf.Gain, float.Epsilon);
            return;
        }
        #endregion Platform hack for MacOS

        AL.Listener(ALListenerf.Gain, effectiveGain);
    }

    private void SetMasterFadeTarget(float fadeGain)
    {
        if (MathF.Abs(_masterFadeTargetGain - fadeGain) < 0.001f)
            return;

        _masterFadeStartGain = FadeGain;
        _masterFadeTargetGain = fadeGain;
        _masterFadeElapsed = 0f;
        ApplyMasterGain();
    }

    #endregion

    #region Context & HRTF

    private void AudioCreateContext()
    {
        _alContextExtensions.Clear();
        var extensions = ALC.GetString(_openALDevice, AlcGetString.Extensions) ?? "";
        foreach (var extension in extensions.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            _alContextExtensions.Add(extension);

        _openALContext = ALC.CreateContext(_openALDevice, BuildContextAttributes());
        if (!ALC.MakeContextCurrent(_openALContext))
        {
            OpenALSawmill.Error("Failed to make OpenAL context current.");
            ALC.DestroyContext(_openALContext);
            _openALContext = ALContext.Null;
            return;
        }
        CheckAlcError(_openALDevice);
        CheckAlError();

        LogHrtfStatus();
    }

    private int[] BuildContextAttributes()
    {
        if (!_cfg.GetCVar(CVars.AudioHrtf) || !HasAlContextExtension("ALC_SOFT_HRTF"))
            return [0];

        var hrtfCount = ALC.GetInteger(_openALDevice, (AlcGetInteger)AlcSoftGetInteger.NumHrtfSpecifiers);
        if (hrtfCount <= 0)
        {
            OpenALSawmill.Warning("HRTF is enabled but no specifiers are supported, HRTF will be disabled.");
            return [0];
        }

        return
        [
            (int) AlcSoftGetInteger.Hrtf, 1,
            (int) AlcSoftGetInteger.HrtfId, 0,
            0
        ];
    }

    private void LogHrtfStatus()
    {
        var supportsHrtf = HasAlContextExtension("ALC_SOFT_HRTF");
        var hrtf = supportsHrtf
            ? ALC.GetInteger(_openALDevice, (AlcGetInteger)AlcSoftGetInteger.HrtfStatus)
            : 0;

        OpenALSawmill.Debug($"OpenAL vendor: {AL.Get(ALGetString.Vendor)}, " +
                            $"renderer: {AL.Get(ALGetString.Renderer)}, " +
                            $"version: {AL.Get(ALGetString.Version)}, " +
                            $"HRTF: {(hrtf == 1 ? "enabled" : "disabled")}");
    }

    private ClydeHandle RegisterBuffer(int buffer)
    {
        _audioSampleBuffers.Add(buffer, new LoadedAudioSample(buffer));
        return new ClydeHandle(Interlocked.Increment(ref _nextClydeHandle));
    }

    #endregion

    #region Reload, Distance Model, Threading, EFX

    private void OnReload(ResPath args)
    {
        if (args.Extension != "ogg" &&
            args.Extension != "wav")
            return;

        _cache.ReloadResource<AudioResource>(args);
    }

    private void ApplyDistanceModel()
    {
        var model = _attenuation switch
        {
            Attenuation.NoAttenuation => ALDistanceModel.None,
            Attenuation.InverseDistance => ALDistanceModel.InverseDistance,
            Attenuation.InverseDistanceClamped => ALDistanceModel.InverseDistanceClamped,
            Attenuation.LinearDistance => ALDistanceModel.LinearDistance,
            Attenuation.LinearDistanceClamped => ALDistanceModel.LinearDistanceClamped,
            Attenuation.ExponentDistance => ALDistanceModel.ExponentDistance,
            Attenuation.ExponentDistanceClamped => ALDistanceModel.ExponentDistanceClamped,
            _ => throw new ArgumentOutOfRangeException($"No DistanceModel mapping for {_attenuation}!")
        };

        AL.DistanceModel(model);
        OpenALSawmill.Info($"Set audio attenuation to {_attenuation}");
    }

    /// <summary>Whether the calling thread is the game thread audio was initialized on.</summary>
    internal bool IsMainThread() => Thread.CurrentThread == _gameThread;

    private static void RemoveEfx((int sourceHandle, int filterHandle) handles)
    {
        if (handles.filterHandle != 0)
            ALC.EFX.DeleteFilter(handles.filterHandle);
    }

    #endregion

    #region Error Checking & Logging

    private void CheckAlcError(ALDevice device, [CallerMemberName] string callerMember = "", [CallerLineNumber] int callerLineNumber = -1)
    {
        if (device == ALDevice.Null)
            return;

        var error = ALC.GetError(device);
        if (error != AlcError.NoError)
            OpenALSawmill.Error("[{0}:{1}] ALC error: {2}", callerMember, callerLineNumber, error);
    }

    /// <summary>Logs a plain error message to the audio sawmill.</summary>
    internal void LogError(string message) => OpenALSawmill.Error(message);

    /// <summary>
    /// Like <c>CheckAlError</c> but allows a custom, lazily-formatted message to be attached.
    /// The interpolated string is only built when an AL error is actually pending.
    /// </summary>
    internal void LogALError(ALErrorInterpolatedStringHandler message, [CallerMemberName] string callerMember = "", [CallerLineNumber] int callerLineNumber = -1)
    {
        if (message.Error != ALError.NoError)
            OpenALSawmill.Error("[{0}:{1}] AL error: {2}, {3}. Stacktrace is {4}", callerMember, callerLineNumber, message.Error, message.ToStringAndClear(), Environment.StackTrace);
    }

    public void CheckAlError([CallerMemberName] string callerMember = "", [CallerLineNumber] int callerLineNumber = -1)
    {
        var error = AL.GetError();
        if (error != ALError.NoError)
        {
            OpenALSawmill.Error("[{0}:{1}] AL error: {2}", callerMember, callerLineNumber, error);
        }
    }

    #endregion

    #region Nested Types

    /*
     * Evil hack because OpenTK doesn't expose the device switch call.
     */

    private static TDelegate? LoadAlcDelegate<TDelegate>(string name)
        where TDelegate : Delegate
    {
        var type = typeof(ALC);
        while (type != null)
        {
            var method = type.GetMethod(
                "LoadDelegate",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

            if (method != null)
                return (TDelegate?)method.MakeGenericMethod(typeof(TDelegate)).Invoke(null, [name]);

            type = type.BaseType;
        }

        return null;
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

    private sealed class LoadedAudioSample(int bufferHandle)
    {
        public readonly int BufferHandle = bufferHandle;
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

    #endregion
}
