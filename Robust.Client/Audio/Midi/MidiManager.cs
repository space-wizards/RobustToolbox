using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using NFluidsynth;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Utility;
using Logger = Robust.Shared.Log.Logger;

namespace Robust.Client.Audio.Midi
{
    public interface IMidiManager
    {
        /// <summary>
        ///     This method tries to return a midi renderer ready to be used.
        ///     You only need to set the <see cref="IMidiRenderer.MidiProgram"/> afterwards.
        /// </summary>
        /// <remarks>
        ///     This method can fail if MIDI support is not available.
        /// </remarks>
        /// <returns>
        ///     <c>null</c> if MIDI support is not available.
        /// </returns>
        IMidiRenderer? GetNewRenderer();

        /*
        /// <summary>
        ///     Checks whether the file at the given path is a valid midi file or not.
        /// </summary>
        /// <remarks>
        ///     We add this here so content doesn't need to reference NFluidsynth.
        /// </remarks>
        bool IsMidiFile(string filename);

        /// <summary>
        ///     Checks whether the file at the given path is a valid midi file or not.
        /// </summary>
        /// <remarks>
        ///     We add this here so content doesn't need to reference NFluidsynth.
        /// </remarks>
        bool IsSoundfontFile(string filename);
        */

        /// <summary>
        ///     Method called every frame.
        ///     Should be used to update positional audio.
        /// </summary>
        /// <param name="frameTime"></param>
        void FrameUpdate(float frameTime);

        /// <summary>
        ///     If true, MIDI support is available.
        /// </summary>
        bool IsAvailable { get; }

        public int OcclusionCollisionMask { get; set; }

        void Shutdown();
    }

    internal class MidiManager : IMidiManager
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IResourceManagerInternal _resourceManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private SharedBroadPhaseSystem _broadPhaseSystem = default!;

        public bool IsAvailable
        {
            get
            {
                InitializeFluidsynth();

                return FluidsynthInitialized;
            }
        }

        private readonly List<MidiRenderer> _renderers = new();

        private bool _alive = true;
        private Settings? _settings;
        private Thread? _midiThread;
        private ISawmill _midiSawmill = default!;

        private static readonly string[] LinuxSoundfonts =
        {
            "/usr/share/soundfonts/default.sf2",
            "/usr/share/soundfonts/FluidR3_GM.sf2",
            "/usr/share/soundfonts/freepats-general-midi.sf2",
            "/usr/share/sounds/sf2/default.sf2",
            "/usr/share/sounds/sf2/FluidR3_GM.sf2",
            "/usr/share/sounds/sf2/TimGM6mb.sf2",
        };

        private const string WindowsSoundfont = @"C:\WINDOWS\system32\drivers\gm.dls";

        private const string OsxSoundfont =
            "/System/Library/Components/CoreAudio.component/Contents/Resources/gs_instruments.dls";

        private const string FallbackSoundfont = "/Midi/fallback.sf2";

        private readonly ResourceLoaderCallbacks _soundfontLoaderCallbacks = new();

        private bool FluidsynthInitialized;
        private bool _failedInitialize;

        private NFluidsynth.Logger.LoggerDelegate _loggerDelegate = default!;
        private ISawmill _sawmill = default!;

        public int OcclusionCollisionMask { get; set; }

        private void InitializeFluidsynth()
        {
            if (FluidsynthInitialized || _failedInitialize) return;

            _midiSawmill = Logger.GetSawmill("midi");
            _sawmill = Logger.GetSawmill("midi.fluidsynth");
            _loggerDelegate = LoggerDelegate;

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
                _settings["synth.overflow.age"].DoubleValue = 3000;
                _settings["audio.driver"].StringValue = "file";
                _settings["audio.periods"].IntValue = 8;
                _settings["audio.period-size"].IntValue = 4096;
                _settings["midi.autoconnect"].IntValue = 1;
                _settings["player.reset-synth"].IntValue = 0;
                _settings["synth.midi-bank-select"].StringValue = "gm";
            }
            catch (Exception e)
            {
                Logger.WarningS("midi",
                    "Failed to initialize fluidsynth due to exception, disabling MIDI support:\n{0}", e);
                _failedInitialize = true;
                return;
            }

            _midiThread = new Thread(ThreadUpdate);
            _midiThread.Start();

            _broadPhaseSystem = EntitySystem.Get<SharedBroadPhaseSystem>();
            FluidsynthInitialized = true;
        }

        private void LoggerDelegate(NFluidsynth.Logger.LogLevel level, string message, IntPtr data)
        {
            var rLevel = level switch {
                NFluidsynth.Logger.LogLevel.Panic => LogLevel.Error,
                NFluidsynth.Logger.LogLevel.Error => LogLevel.Error,
                NFluidsynth.Logger.LogLevel.Warning => LogLevel.Warning,
                NFluidsynth.Logger.LogLevel.Information => LogLevel.Info,
                NFluidsynth.Logger.LogLevel.Debug => LogLevel.Debug,
                _ => LogLevel.Debug
            };
            _sawmill.Log(rLevel, message);
        }

        /*
        public bool IsMidiFile(string filename)
        {
            return SoundFont.IsMidiFile(filename);
        }

        public bool IsSoundfontFile(string filename)
        {
            return SoundFont.IsSoundFont(filename);
        }
        */

        public IMidiRenderer? GetNewRenderer()
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

                var renderer = new MidiRenderer(_settings!, soundfontLoader);

                foreach (var file in _resourceManager.ContentFindFiles(("/Audio/MidiCustom/")))
                {
                    if (file.Extension != "sf2" && file.Extension != "dls") continue;
                    renderer.LoadSoundfont(file.ToString());
                }

                // Since the last loaded soundfont takes priority, we load the fallback soundfont before the soundfont.
                renderer.LoadSoundfont(FallbackSoundfont);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    foreach (var filepath in LinuxSoundfonts)
                    {
                        if (!File.Exists(filepath) || !SoundFont.IsSoundFont(filepath)) continue;

                        try
                        {
                            renderer.LoadSoundfont(filepath, true);
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        break;
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (File.Exists(OsxSoundfont) && SoundFont.IsSoundFont(OsxSoundfont))
                        renderer.LoadSoundfont(OsxSoundfont, true);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (File.Exists(WindowsSoundfont) && SoundFont.IsSoundFont(WindowsSoundfont))
                        renderer.LoadSoundfont(WindowsSoundfont, true);
                }

                lock (_renderers)
                    _renderers.Add(renderer);

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
                foreach (var renderer in _renderers)
                {
                    if (renderer.Disposed)
                        continue;

                    if (!renderer.Mono)
                    {
                        renderer.Source.SetGlobal();
                        continue;
                    }

                    MapCoordinates? mapPos = null;
                    if (renderer.TrackingCoordinates != null)
                    {
                        mapPos = renderer.TrackingCoordinates.Value.ToMap(_entityManager);
                    }
                    else if (renderer.TrackingEntity != null)
                    {
                        mapPos = renderer.TrackingEntity.Transform.MapPosition;
                    }

                    if (mapPos != null)
                    {
                        var pos = mapPos.Value;
                        if (pos.MapId != _eyeManager.CurrentMap)
                        {
                            renderer.Source.SetVolume(-10000000);
                        }
                        else
                        {
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
                                    sourceRelative.Length,
                                    renderer.TrackingEntity);
                            }
                            renderer.Source.SetOcclusion(occlusion);
                        }

                        if (renderer.Source.SetPosition(pos.Position))
                        {
                            continue;
                        }

                        if (renderer.TrackingEntity != null)
                        {
                            renderer.Source.SetVelocity(renderer.TrackingEntity.GlobalLinearVelocity());
                        }

                        if (float.IsNaN(pos.Position.X) || float.IsNaN(pos.Position.Y))
                        {
                            // just duck out instead of move to NaN
                            renderer.Source.SetOcclusion(float.MaxValue);
                            continue;
                        }

                        _midiSawmill?.Warning("Interrupting positional audio, can't set position.");
                        renderer.Source.StopPlaying();
                    }
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
                    for (var i = 0; i < _renderers.Count; i++)
                    {
                        var renderer = _renderers[i];
                        if (!renderer.Disposed)
                            renderer.Render();
                        else
                        {
                            ((IMidiRenderer)renderer).InternalDispose();
                            _renderers.Remove(renderer);
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
            foreach (var renderer in _renderers)
            {
                renderer?.Dispose();
            }

            if (FluidsynthInitialized && !_failedInitialize)
            {
                NFluidsynth.Logger.SetLoggerMethod(null);
            }
        }

        /// <summary>
        ///     This class is used to load soundfonts.
        /// </summary>
        private class ResourceLoaderCallbacks : SoundFontLoaderCallbacks
        {
            private readonly Dictionary<int, Stream> _openStreams = new();
            private int _nextStreamId = 1;

            public override IntPtr Open(string filename)
            {
                if (string.IsNullOrEmpty(filename))
                {
                    return IntPtr.Zero;
                }

                Stream? stream;
                var resourceCache = IoCManager.Resolve<IResourceCache>();
                var resourcePath = new ResourcePath(filename);

                if (resourcePath.IsRooted && resourceCache.ContentFileExists(filename))
                {
                    if (!resourceCache.TryContentFileRead(filename, out stream))
                        return IntPtr.Zero;
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
                var stream = _openStreams[(int) sfHandle];
                stream.Dispose();
                _openStreams.Remove((int) sfHandle);
                return 0;
            }
        }
    }
}
