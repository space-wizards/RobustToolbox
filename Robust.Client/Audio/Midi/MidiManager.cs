using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using NFluidsynth;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Audio.Midi;

internal sealed partial class MidiManager : IMidiManager
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IResourceCacheInternal _resourceManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IConfigurationManager _cfgMan = default!;
    [Dependency] private readonly IClydeAudio _clydeAudio = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly ILogManager _logger = default!;

    private SharedPhysicsSystem _broadPhaseSystem = default!;

    public IReadOnlyList<IMidiRenderer> Renderers
    {
        get
        {
            lock (_renderers)
            {
                // Perform a copy. Sadly, we can't return a reference to the original list due to threading concerns.
                return _renderers.ToArray();
            }
        }
    }

    [ViewVariables]
    public bool IsAvailable
    {
        get
        {
            InitializeFluidsynth();

            return FluidsynthInitialized;
        }
    }

    [ViewVariables]
    private readonly List<IMidiRenderer> _renderers = new();

    private bool _alive = true;
    private Settings? _settings;
    private Thread? _midiThread;
    private ISawmill _midiSawmill = default!;
    private float _volume = 0f;
    private bool _volumeDirty = true;

    // Not reliable until Fluidsynth is initialized!
    [ViewVariables(VVAccess.ReadWrite)]
    public float Volume
    {
        get => _volume;
        set
        {
            if (MathHelper.CloseToPercent(_volume, value))
                return;

            _cfgMan.SetCVar(CVars.MidiVolume, value);
            _volumeDirty = true;
        }
    }

    private static readonly string[] LinuxSoundfonts =
    {
        "/usr/share/soundfonts/default.sf2",
        "/usr/share/soundfonts/default.dls",
        "/usr/share/soundfonts/FluidR3_GM.sf2",
        "/usr/share/soundfonts/freepats-general-midi.sf2",
        "/usr/share/sounds/sf2/default.sf2",
        "/usr/share/sounds/sf2/default.dls",
        "/usr/share/sounds/sf2/FluidR3_GM.sf2",
        "/usr/share/sounds/sf2/TimGM6mb.sf2",
    };

    private const string WindowsSoundfont = @"C:\WINDOWS\system32\drivers\gm.dls";

    private const string OsxSoundfont =
        "/System/Library/Components/CoreAudio.component/Contents/Resources/gs_instruments.dls";

    private const string FallbackSoundfont = "/Midi/fallback.sf2";

    private const string ContentCustomSoundfontDirectory = "/Audio/MidiCustom/";

    private const float MaxDistanceForOcclusion = 1000;

    private static ResourcePath CustomSoundfontDirectory = new ResourcePath("/soundfonts/");

    private readonly ResourceLoaderCallbacks _soundfontLoaderCallbacks;

    private bool FluidsynthInitialized;
    private bool _failedInitialize;

    private NFluidsynth.Logger.LoggerDelegate _loggerDelegate = default!;
    private ISawmill _sawmill = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public int OcclusionCollisionMask { get; set; }

    public MidiManager()
    {
        _soundfontLoaderCallbacks = new ResourceLoaderCallbacks(this);
    }

    private void InitializeFluidsynth()
    {
        if (FluidsynthInitialized || _failedInitialize) return;

        _volume = _cfgMan.GetCVar(CVars.MidiVolume);
        _cfgMan.OnValueChanged(CVars.MidiVolume, value =>
        {
            _volume = value;
            _volumeDirty = true;
        }, true);

        _midiSawmill = _logger.GetSawmill("midi");
#if DEBUG
        _midiSawmill.Level = LogLevel.Debug;
#else
        _midiSawmill.Level = LogLevel.Error;
#endif
        _sawmill = _logger.GetSawmill("midi.fluidsynth");
        _loggerDelegate = LoggerDelegate;

        if (!_resourceManager.UserData.Exists(CustomSoundfontDirectory))
        {
            _resourceManager.UserData.CreateDir(CustomSoundfontDirectory);
        }
        // not a directory, preserve the old file and create an actual directory
        else if (!_resourceManager.UserData.IsDir(CustomSoundfontDirectory))
        {
            _resourceManager.UserData.Rename(CustomSoundfontDirectory, CustomSoundfontDirectory.WithName(CustomSoundfontDirectory.Filename + ".old"));
            _resourceManager.UserData.CreateDir(CustomSoundfontDirectory);
        }

        try
        {
            NFluidsynth.Logger.SetLoggerMethod(_loggerDelegate); // Will cause a safe DllNotFoundException if not available.

            _settings = new Settings();
            _settings["synth.sample-rate"].DoubleValue = 44100;
            _settings["player.timing-source"].StringValue = "sample";
            _settings["synth.lock-memory"].IntValue = 0;
            _settings["synth.threadsafe-api"].IntValue = 1;
            _settings["synth.gain"].DoubleValue = 1.0d;
            _settings["synth.polyphony"].IntValue = 1024;
            _settings["synth.cpu-cores"].IntValue = 2;
            _settings["synth.midi-channels"].IntValue = 16;
            _settings["synth.overflow.age"].DoubleValue = 3000;
            _settings["audio.driver"].StringValue = "file";
            _settings["audio.periods"].IntValue = 8;
            _settings["audio.period-size"].IntValue = 4096;
            _settings["midi.autoconnect"].IntValue = 1;
            _settings["player.reset-synth"].IntValue = 0;
            _settings["synth.midi-bank-select"].StringValue = "gm";
            //_settings["synth.verbose"].IntValue = 1; // Useful for debugging.
        }
        catch (Exception e)
        {
            _midiSawmill.Error("Failed to initialize fluidsynth due to exception, disabling MIDI support:\n{0}", e);
            _failedInitialize = true;
            return;
        }

        _midiThread = new Thread(ThreadUpdate);
        _midiThread.Start();

        _broadPhaseSystem = _entityManager.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>();
        FluidsynthInitialized = true;
    }

    private void LoggerDelegate(NFluidsynth.Logger.LogLevel level, string message, IntPtr data)
    {
        var rLevel = level switch
        {
            NFluidsynth.Logger.LogLevel.Panic => LogLevel.Error,
            NFluidsynth.Logger.LogLevel.Error => LogLevel.Error,
            NFluidsynth.Logger.LogLevel.Warning => LogLevel.Warning,
            NFluidsynth.Logger.LogLevel.Information => LogLevel.Info,
            NFluidsynth.Logger.LogLevel.Debug => LogLevel.Debug,
            _ => LogLevel.Debug
        };
        _sawmill.Log(rLevel, message);
    }

    public IMidiRenderer? GetNewRenderer(bool mono = true)
    {
        if (!FluidsynthInitialized)
        {
            InitializeFluidsynth();

            if (!FluidsynthInitialized) // init failed
            {
                return null;
            }
        }

        var soundfontLoader = SoundFontLoader.NewDefaultSoundFontLoader(_settings);

        // Just making double sure these don't get GC'd.
        // They shouldn't, MidiRenderer keeps a ref, but making sure...
        var handle = GCHandle.Alloc(soundfontLoader);

        try
        {
            soundfontLoader.SetCallbacks(_soundfontLoaderCallbacks);

            var renderer = new MidiRenderer(_settings!, soundfontLoader, mono, this, _clydeAudio, _taskManager, _midiSawmill);

            _midiSawmill.Debug($"Loading soundfont {FallbackSoundfont}");
            // Since the last loaded soundfont takes priority, we load the fallback soundfont before the soundfont.
            renderer.LoadSoundfont(FallbackSoundfont);

            if (OperatingSystem.IsLinux())
            {
                foreach (var filepath in LinuxSoundfonts)
                {
                    if (!File.Exists(filepath) || !SoundFont.IsSoundFont(filepath))
                        continue;

                    try
                    {
                        renderer.LoadSoundfont(filepath);
                        _midiSawmill.Debug($"Loaded Linux soundfont {filepath}");
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    break;
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                if (File.Exists(OsxSoundfont) && SoundFont.IsSoundFont(OsxSoundfont))
                {
                    _midiSawmill.Debug($"Loading soundfont {OsxSoundfont}");
                    renderer.LoadSoundfont(OsxSoundfont);
                }
            }
            else if (OperatingSystem.IsWindows())
            {
                if (File.Exists(WindowsSoundfont) && SoundFont.IsSoundFont(WindowsSoundfont))
                {
                    _midiSawmill.Debug($"Loading soundfont {WindowsSoundfont}");
                    renderer.LoadSoundfont(WindowsSoundfont);
                }
            }

            // Load content-specific custom soundfonts, which could override the system/fallback soundfont.
            _midiSawmill.Debug($"Loading soundfonts from {ContentCustomSoundfontDirectory}");
            foreach (var file in _resourceManager.ContentFindFiles(ContentCustomSoundfontDirectory))
            {
                if (file.Extension != "sf2" && file.Extension != "dls") continue;
                _midiSawmill.Debug($"Loading soundfont {file}");
                renderer.LoadSoundfont(file.ToString());
            }

            // Load every soundfont from the user data directory last, since those may override any other soundfont.
            _midiSawmill.Debug($"Loading soundfonts from {{USERDATA}} {CustomSoundfontDirectory}");
            var enumerator = _resourceManager.UserData.Find($"{CustomSoundfontDirectory.ToRelativePath()}/*").Item1;
            foreach (var file in enumerator)
            {
                if (file.Extension != "sf2" && file.Extension != "dls") continue;
                _midiSawmill.Debug($"Loading soundfont {{USERDATA}} {file}");
                renderer.LoadSoundfont(file.ToString());
            }

            renderer.Source.SetVolume(Volume);

            lock (_renderers)
            {
                _renderers.Add(renderer);
            }
            return renderer;
        }
        finally
        {
            handle.Free();
        }
    }

    public void FrameUpdate(float frameTime)
    {
        if (!FluidsynthInitialized)
        {
            return;
        }

        // Update positions of streams every frame.
        lock (_renderers)
        {
            foreach (var renderer in _renderers)
            {
                if (renderer.Disposed)
                    continue;

                if(_volumeDirty)
                    renderer.Source.SetVolume(Volume);

                if (!renderer.Mono)
                {
                    renderer.Source.SetGlobal();
                    continue;
                }

                MapCoordinates? mapPos = null;
                var trackingEntity = renderer.TrackingEntity != null && !_entityManager.Deleted(renderer.TrackingEntity);
                if (trackingEntity)
                {
                    renderer.TrackingCoordinates = _entityManager.GetComponent<TransformComponent>(renderer.TrackingEntity!.Value).Coordinates;
                }

                if (renderer.TrackingCoordinates != null)
                {
                    mapPos = renderer.TrackingCoordinates.Value.ToMap(_entityManager);
                }

                if (mapPos != null && mapPos.Value.MapId == _eyeManager.CurrentMap)
                {
                    var pos = mapPos.Value;

                    var sourceRelative = _eyeManager.CurrentEye.Position.Position - pos.Position;
                    var occlusion = 0f;
                    if (sourceRelative.Length > 0)
                    {
                        occlusion = _broadPhaseSystem.IntersectRayPenetration(
                            pos.MapId,
                            new CollisionRay(
                                pos.Position,
                                sourceRelative.Normalized,
                                OcclusionCollisionMask),
                            MathF.Min(sourceRelative.Length, MaxDistanceForOcclusion),
                            renderer.TrackingEntity);
                    }

                    renderer.Source.SetOcclusion(occlusion);

                    if (!renderer.Source.SetPosition(pos.Position))
                    {
                        return;
                    }

                    if (trackingEntity)
                    {
                        renderer.Source.SetVelocity(renderer.TrackingEntity!.Value.GlobalLinearVelocity());
                    }
                }
                else
                {
                    renderer.Source.SetOcclusion(float.MaxValue);
                }
            }
        }

        _volumeDirty = false;
    }

    /// <summary>
    ///     Main method for the thread rendering the midi audio.
    /// </summary>
    private void ThreadUpdate()
    {
        while (_alive)
        {
            lock (_renderers)
            {
                for (var i = 0; i < _renderers.Count; i++)
                {
                    var renderer = _renderers[i];
                    if (!renderer.Disposed)
                        renderer.Render();
                    else
                    {
                        renderer.InternalDispose();
                        _renderers.Remove(renderer);
                    }
                }
            }

            Thread.Sleep(1);
        }
    }

    public void Shutdown()
    {
        _alive = false;
        _midiThread?.Join();
        _settings?.Dispose();

        lock (_renderers)
        {
            foreach (var renderer in _renderers)
            {
                renderer?.Dispose();
            }
        }

        if (FluidsynthInitialized && !_failedInitialize)
        {
            NFluidsynth.Logger.SetLoggerMethod(null);
        }
    }

    /// <summary>
    ///     Internal method to get a human-readable representation of a <see cref="SequencerEvent"/>.
    /// </summary>
    internal static string SequencerEventToString(SequencerEvent midiEvent)
    {
        // ReSharper disable once UseStringInterpolation
        return string.Format(
            "{0} chan:{1:D2} key:{2:D5} bank:{3:D2} ctrl:{4:D5} dur:{5:D5} pitch:{6:D5} prog:{7:D3} val:{8:D5} vel:{9:D5}",
            midiEvent.Type.ToString().PadLeft(22),
            midiEvent.Channel,
            midiEvent.Key,
            midiEvent.Bank,
            midiEvent.Control,
            midiEvent.Duration,
            midiEvent.Pitch,
            midiEvent.Program,
            midiEvent.Value,
            midiEvent.Velocity);
    }

    /// <summary>
    ///     This class is used to load soundfonts.
    /// </summary>
    private sealed class ResourceLoaderCallbacks : SoundFontLoaderCallbacks
    {
        private readonly MidiManager _parent;
        private readonly Dictionary<int, Stream> _openStreams = new();
        private int _nextStreamId = 1;

        public ResourceLoaderCallbacks(MidiManager parent)
        {
            _parent = parent;
        }

        public override IntPtr Open(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return IntPtr.Zero;
            }

            Stream? stream;
            var resourceCache = _parent._resourceManager;
            var resourcePath = new ResourcePath(filename);

            if (resourcePath.IsRooted)
            {
                // is it in content?
                if (resourceCache.ContentFileExists(filename))
                {
                    if (!resourceCache.TryContentFileRead(filename, out stream))
                        return IntPtr.Zero;
                }
                // is it in userdata?
                else if (resourceCache.UserData.Exists(resourcePath))
                {
                    stream = resourceCache.UserData.OpenRead(resourcePath);
                }
                else if (File.Exists(filename))
                {
                    stream = File.OpenRead(filename);
                }
                else
                {
                    return IntPtr.Zero;
                }
            }
            else if (File.Exists(filename))
            {
                stream = File.OpenRead(filename);
            }
            else
            {
                return IntPtr.Zero;
            }

            var id = _nextStreamId++;

            _openStreams.Add(id, stream);

            return (IntPtr) id;
        }

        public override unsafe int Read(IntPtr buf, long count, IntPtr sfHandle)
        {
            var length = (int) count;
            var span = new Span<byte>(buf.ToPointer(), length);
            var stream = _openStreams[(int) sfHandle];

            // Fluidsynth's docs state that this method should leave the buffer unmodified if it fails. (returns -1)
            try
            {
                // Fluidsynth does a LOT of tiny allocations (frankly, way too much).
                if (count < 1024)
                {
                    // ReSharper disable once SuggestVarOrType_Elsewhere
                    Span<byte> buffer = stackalloc byte[(int)count];

                    stream.ReadExact(buffer);

                    buffer.CopyTo(span);
                }
                else
                {
                    var buffer = stream.ReadExact(length);

                    buffer.CopyTo(span);
                }
            }
            catch (EndOfStreamException)
            {
                return -1;
            }

            return 0;
        }

        public override int Seek(IntPtr sfHandle, int offset, SeekOrigin origin)
        {
            var stream = _openStreams[(int) sfHandle];

            stream.Seek(offset, origin);

            return 0;
        }

        public override int Tell(IntPtr sfHandle)
        {
            var stream = _openStreams[(int) sfHandle];

            return (int) stream.Position;
        }

        public override int Close(IntPtr sfHandle)
        {
            if (!_openStreams.Remove((int) sfHandle, out var stream))
                return -1;

            stream.Dispose();
            return 0;

        }
    }
}
