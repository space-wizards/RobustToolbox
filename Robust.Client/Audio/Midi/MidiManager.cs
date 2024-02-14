using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NFluidsynth;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Audio.Midi;

internal sealed partial class MidiManager : IMidiManager
{
    public const string SoundfontEnvironmentVariable = "ROBUST_SOUNDFONT_OVERRIDE";

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IConfigurationManager _cfgMan = default!;
    [Dependency] private readonly IAudioInternal _audio = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly ILogManager _logger = default!;
    [Dependency] private readonly IParallelManager _parallel = default!;
    [Dependency] private readonly IRuntimeLog _runtime = default!;

    private AudioSystem _audioSys = default!;
    private SharedPhysicsSystem _broadPhaseSystem = default!;
    private SharedTransformSystem _xformSystem = default!;

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

    [ViewVariables] private readonly List<IMidiRenderer> _renderers = new();

    // To avoid lock contention until some kind of MIDI refactor.
    private TimeSpan _nextUpdate;
    private TimeSpan _updateFrequency = TimeSpan.FromSeconds(0.25f);

    private SemaphoreSlim _updateSemaphore = new(1);

    private bool _alive = true;
    [ViewVariables] private Settings? _settings;
    private Thread? _midiThread;
    private ISawmill _midiSawmill = default!;
    private float _gain = 0f;
    private bool _volumeDirty = true;

    // Not reliable until Fluidsynth is initialized!
    [ViewVariables(VVAccess.ReadWrite)]
    public float Gain
    {
        get => _gain;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);

            if (MathHelper.CloseToPercent(_gain, clamped))
                return;

            _cfgMan.SetCVar(CVars.MidiVolume, clamped);
            _volumeDirty = true;
        }
    }

    private static readonly string[] LinuxSoundfonts =
    {
        "/usr/share/soundfonts/default.sf2",
        "/usr/share/soundfonts/default.dls",
        "/usr/share/soundfonts/FluidR3_GM.sf2",
        "/usr/share/soundfonts/FluidR3_GM2-2.sf2",
        "/usr/share/soundfonts/freepats-general-midi.sf2",
        "/usr/share/sounds/sf2/default.sf2",
        "/usr/share/sounds/sf2/default.dls",
        "/usr/share/sounds/sf2/FluidR3_GM.sf2",
        "/usr/share/sounds/sf2/FluidR3_GM2-2.sf2",
        "/usr/share/sounds/sf2/TimGM6mb.sf2",
    };

    private static readonly string WindowsSoundfont = $@"{Environment.GetEnvironmentVariable("SystemRoot")}\system32\drivers\gm.dls";

    private const string OsxSoundfont =
        "/System/Library/Components/CoreAudio.component/Contents/Resources/gs_instruments.dls";

    private const string FallbackSoundfont = "/Midi/fallback.sf2";

    private const string ContentCustomSoundfontDirectory = "/Audio/MidiCustom/";

    private static ResPath CustomSoundfontDirectory = new("/soundfonts/");

    private readonly ResourceLoaderCallbacks _soundfontLoaderCallbacks;

    private bool FluidsynthInitialized;
    private bool _failedInitialize;

    private NFluidsynth.Logger.LoggerDelegate _loggerDelegate = default!;
    private ISawmill _fluidsynthSawmill = default!;

    private MidiUpdateJob _updateJob;


    public MidiManager()
    {
        _soundfontLoaderCallbacks = new ResourceLoaderCallbacks(this);
    }

    private void InitializeFluidsynth()
    {
        if (FluidsynthInitialized || _failedInitialize) return;

        _cfgMan.OnValueChanged(CVars.MidiVolume, value =>
        {
            _gain = value;
            _volumeDirty = true;
        }, true);

        _midiSawmill = _logger.GetSawmill("midi");
#if DEBUG
        _midiSawmill.Level = LogLevel.Debug;
#else
        _midiSawmill.Level = LogLevel.Error;
#endif
        _fluidsynthSawmill = _logger.GetSawmill("midi.fluidsynth");
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
            _settings["synth.midi-channels"].IntValue = 16;
            _settings["synth.overflow.age"].DoubleValue = 3000;
            _settings["audio.driver"].StringValue = "file";
            _settings["audio.periods"].IntValue = 8;
            _settings["audio.period-size"].IntValue = 4096;
            _settings["midi.autoconnect"].IntValue = 1;
            _settings["player.reset-synth"].IntValue = 0;
            _settings["synth.midi-channels"].IntValue = Math.Clamp(RobustMidiEvent.MaxChannels, 16, 256);
            _settings["synth.midi-bank-select"].StringValue = "gm";
            //_settings["synth.verbose"].IntValue = 1; // Useful for debugging.

            var midiParallel = _cfgMan.GetCVar(CVars.MidiParallelism);
            _settings["synth.polyphony"].IntValue = Math.Clamp(1024 + (int)(Math.Log2(midiParallel) * 2048), 1, 65535);
            _settings["synth.cpu-cores"].IntValue = Math.Clamp(midiParallel, 1, 256);

            _midiSawmill.Debug($"Synth Cores: {_settings["synth.cpu-cores"].IntValue}");
            _midiSawmill.Debug($"Synth Polyphony: {_settings["synth.polyphony"].IntValue}");
        }
        catch (Exception e)
        {
            _midiSawmill.Error("Failed to initialize fluidsynth due to exception, disabling MIDI support:\n{0}", e);
            _failedInitialize = true;
            return;
        }

        _midiThread = new Thread(ThreadUpdate)
        {
            Name = "RobustToolbox MIDI Thread"
        };
        _midiThread.Start();

        _updateJob = new MidiUpdateJob()
        {
            Manager = this,
            Renderers = _renderers,
        };

        _audioSys = _entityManager.EntitySysManager.GetEntitySystem<AudioSystem>();
        _broadPhaseSystem = _entityManager.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>();
        _xformSystem = _entityManager.System<SharedTransformSystem>();
        _entityManager.GetEntityQuery<PhysicsComponent>();
        _entityManager.GetEntityQuery<TransformComponent>();

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
        _fluidsynthSawmill.Log(rLevel, message);
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

            var renderer = new MidiRenderer(_settings!, soundfontLoader, mono, this, _audio, _taskManager, _midiSawmill);

            _midiSawmill.Debug($"Loading fallback soundfont {FallbackSoundfont}");
            // Since the last loaded soundfont takes priority, we load the fallback soundfont before the soundfont.
            renderer.LoadSoundfont(FallbackSoundfont);

            // Load system-specific soundfonts.
            if (OperatingSystem.IsLinux())
            {
                foreach (var filepath in LinuxSoundfonts)
                {
                    if (!File.Exists(filepath) || !SoundFont.IsSoundFont(filepath))
                        continue;

                    try
                    {
                        _midiSawmill.Debug($"Loading OS soundfont {filepath}");
                        renderer.LoadSoundfont(filepath);
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
                    _midiSawmill.Debug($"Loading OS soundfont {OsxSoundfont}");
                    renderer.LoadSoundfont(OsxSoundfont);
                }
            }
            else if (OperatingSystem.IsWindows())
            {
                if (File.Exists(WindowsSoundfont) && SoundFont.IsSoundFont(WindowsSoundfont))
                {
                    _midiSawmill.Debug($"Loading OS soundfont {WindowsSoundfont}");
                    renderer.LoadSoundfont(WindowsSoundfont);
                }
            }

            // Maybe load soundfont specified in environment variable.
            // Load it here so it can override system soundfonts but not content or user data soundfonts.
            if (Environment.GetEnvironmentVariable(SoundfontEnvironmentVariable) is {} soundfontOverride)
            {
                if (File.Exists(soundfontOverride) && SoundFont.IsSoundFont(soundfontOverride))
                {
                    _midiSawmill.Debug($"Loading environment variable soundfont {soundfontOverride}");
                    renderer.LoadSoundfont(soundfontOverride);
                }
            }

            // Load content-specific custom soundfonts, which should override the system/fallback soundfont.
            _midiSawmill.Debug($"Loading soundfonts from content directory {ContentCustomSoundfontDirectory}");
            foreach (var file in _resourceManager.ContentFindFiles(ContentCustomSoundfontDirectory))
            {
                if (file.Extension != "sf2" && file.Extension != "dls" && file.Extension != "sf3") continue;
                _midiSawmill.Debug($"Loading content soundfont {file}");
                renderer.LoadSoundfont(file.ToString());
            }

            var userDataPath = _resourceManager.UserData.RootDir == null
                ? CustomSoundfontDirectory
                : new ResPath(_resourceManager.UserData.RootDir) / CustomSoundfontDirectory.ToRelativePath();

            // Load every soundfont from the user data directory last, since those may override any other soundfont.
            _midiSawmill.Debug($"Loading soundfonts from user data directory {userDataPath}");
            var enumerator = _resourceManager.UserData.Find($"{CustomSoundfontDirectory.ToRelativePath()}*").Item1;
            foreach (var file in enumerator)
            {
                if (file.Extension != "sf2" && file.Extension != "dls" && file.Extension != "sf3") continue;
                _midiSawmill.Debug($"Loading user soundfont {file}");
                renderer.LoadSoundfont(file.ToString());
            }

            renderer.Source.Gain = _gain;

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

        if (_nextUpdate > _timing.RealTime)
            return;

        _nextUpdate = _timing.RealTime + _updateFrequency;

        // Update positions of streams occasionally.
        // This has a lot of code duplication with AudioSystem.FrameUpdate(), and they should probably be combined somehow.
        // so TRUE

        _updateJob.OurPosition = _audioSys.GetListenerCoordinates();

        // This semaphore is here to avoid lock contention as much as possible.
        _updateSemaphore.Wait();

        // The ONLY time this should be contested is with ThreadUpdate.
        // If that becomes NOT the case then just lock this, remove the semaphore, and drop the update frequency even harder.
        // ReSharper disable once InconsistentlySynchronizedField
        _parallel.ProcessNow(_updateJob, _renderers.Count);

        _updateSemaphore.Release();

        _volumeDirty = false;
    }

    private void UpdateRenderer(IMidiRenderer renderer, MapCoordinates listener)
    {
        // TODO: This should be sharing more code with AudioSystem.
        try
        {
            if (renderer.Disposed)
                return;

            if (_volumeDirty)
            {
                renderer.Source.Gain = Gain;
            }

            if (!renderer.Mono)
            {
                renderer.Source.Global = true;
                return;
            }

            MapCoordinates mapPos;

            if (renderer.TrackingEntity is {} trackedEntity && !_entityManager.Deleted(trackedEntity))
            {
                renderer.TrackingCoordinates = _xformSystem.GetMapCoordinates(renderer.TrackingEntity.Value);

                // Pause it if the attached entity is paused.
                if (_entityManager.IsPaused(renderer.TrackingEntity))
                {
                    renderer.Source.Pause();
                    return;
                }
            }
            else if (renderer.TrackingCoordinates == null)
            {
                renderer.Source.Pause();
                return;
            }

            mapPos = renderer.TrackingCoordinates.Value;

            // If it's on a different map then just mute it, not pause.
            if (mapPos.MapId == MapId.Nullspace || mapPos.MapId != listener.MapId)
            {
                renderer.Source.Gain = 0f;
                return;
            }

            // Was previously muted maybe so try unmuting it?
            if (renderer.Source.Gain == 0f)
            {
                renderer.Source.Gain = Gain;
            }

            var worldPos = mapPos.Position;
            var delta = worldPos - listener.Position;
            var distance = delta.Length();

            // Update position
            // Out of range so just clip it for us.
            if (distance > renderer.Source.MaxDistance)
            {
                // Still keeps the source playing, just with no volume.
                renderer.Source.Gain = 0f;
                return;
            }

            // Same imprecision suppression as audiosystem.
            if (distance > 0f && distance < 0.01f)
            {
                worldPos = listener.Position;
                delta = Vector2.Zero;
                distance = 0f;
            }

            renderer.Source.Position = worldPos;

            // Update velocity (doppler).
            if (!_entityManager.Deleted(renderer.TrackingEntity))
            {
                var velocity = _broadPhaseSystem.GetMapLinearVelocity(renderer.TrackingEntity.Value);
                renderer.Source.Velocity = velocity;
            }
            else
            {
                renderer.Source.Velocity = Vector2.Zero;
            }

            // Update occlusion
            var occlusion = _audioSys.GetOcclusion(listener, delta, distance, renderer.TrackingEntity);
            renderer.Source.Occlusion = occlusion;
        }
        catch (Exception ex)
        {
            _runtime.LogException(ex, _midiSawmill.Name);
        }
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
                var toRemove = new ValueList<IMidiRenderer>();

                for (var i = 0; i < _renderers.Count; i++)
                {
                    var renderer = _renderers[i];

                    lock (renderer)
                    {
                        if (!renderer.Disposed)
                        {
                            if (renderer.Master is { Disposed: true })
                                renderer.Master = null;

                            renderer.Render();
                        }
                        else
                        {
                            toRemove.Add(renderer);
                        }
                    }
                }

                if (toRemove.Count > 0)
                {
                    _updateSemaphore.Wait();

                    foreach (var renderer in toRemove)
                    {
                        renderer.InternalDispose();
                        _renderers.Remove(renderer);
                    }

                    _updateSemaphore.Release();
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
            var resourcePath = new ResPath(filename);

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

        public override int Seek(IntPtr sfHandle, long offset, SeekOrigin origin)
        {
            var stream = _openStreams[(int) sfHandle];

            stream.Seek(offset, origin);

            return 0;
        }

        public override long Tell(IntPtr sfHandle)
        {
            var stream = _openStreams[(int) sfHandle];

            return (long) stream.Position;
        }

        public override int Close(IntPtr sfHandle)
        {
            if (!_openStreams.Remove((int) sfHandle, out var stream))
                return -1;

            stream.Dispose();
            return 0;

        }
    }

    #region Jobs

    private record struct MidiUpdateJob : IParallelRobustJob
    {
        public int MinimumBatchParallel => 2;

        public int BatchSize => 1;

        public MidiManager Manager;

        public MapCoordinates OurPosition;
        public List<IMidiRenderer> Renderers;

        public void Execute(int index)
        {
            // The indices shouldn't be able to be touched while this job is running, just the renderer itself getting locked.
            var renderer = Renderers[index];

            lock (renderer)
            {
                Manager.UpdateRenderer(renderer, OurPosition);
            }
        }
    }

    #endregion
}
