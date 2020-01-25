using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using NFluidsynth;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.Audio.Midi
{
    public interface IMidiManager
    {
        /// <summary>
        ///     This method returns a midi renderer ready to be used.
        ///     You only need to set the <see cref="IMidiRenderer.MidiProgram"/> afterwards.
        /// </summary>
        IMidiRenderer GetNewRenderer();

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


        /// <summary>
        ///     Method called every frame.
        ///     Should be used to update positional audio.
        /// </summary>
        /// <param name="frameTime"></param>
        void FrameUpdate(float frameTime);
    }

    internal class MidiManager : IDisposable, IMidiManager
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        private bool _alive = true;
        private Settings _settings;
        private List<MidiRenderer> _renderers = new List<MidiRenderer>();
        private Thread _midiThread;

        private static readonly string[] LinuxSoundfonts =
        {
            "/usr/share/soundfonts/default.sf2",
            "/usr/share/soundfonts/FluidR3_GM.sf2",
            "/usr/share/soundfonts/freepats-general-midi.sf2",
            "/usr/share/sounds/sf2/default.sf2",
            "/usr/share/sounds/sf2/FluidR3_GM.sf2",
            "/usr/share/sounds/sf2/TimGM6mb.sf2",
        };

        private static readonly string WindowsSoundfont = "c:\\WINDOWS\\system32\\drivers\\gm.dls";

        private static readonly string OSXSoundfont =
            "/System/Library/Components/CoreAudio.component/Contents/Resources/gs_instruments.dls";

        private static readonly string FallbackSoundfont = "/Resources/Midi/fallback.sf2";

        private ResourceLoaderCallbacks _soundfontLoaderCallbacks;

        public bool FluidsynthInitialized { get; private set; } = false;

        private void InitializeFluidsynth()
        {
            if (FluidsynthInitialized) return;

                _settings = new Settings();
            _settings["synth.sample-rate"].DoubleValue = 48000;
            _settings["player.timing-source"].StringValue = "sample";
            _settings["synth.lock-memory"].IntValue = 0;
            _settings["synth.threadsafe-api"].IntValue = 1;
            _settings["synth.gain"].DoubleValue = 0.5d;
            _settings["audio.driver"].StringValue = "file";
            _settings["midi.autoconnect"].IntValue = 1;
            _settings["player.reset-synth"].IntValue = 0;
            _settings["synth.midi-bank-select"].StringValue = "gm";

            _midiThread = new Thread(ThreadUpdate);
            _midiThread.Start();

            _soundfontLoaderCallbacks = new ResourceLoaderCallbacks();

            FluidsynthInitialized = true;
        }

        public bool IsMidiFile(string filename)
        {
            return SoundFont.IsMidiFile(filename);
        }

        public bool IsSoundfontFile(string filename)
        {
            return SoundFont.IsSoundFont(filename);
        }

        public IMidiRenderer GetNewRenderer()
        {
            if(!FluidsynthInitialized)
                InitializeFluidsynth();

            var soundfontLoader = SoundFontLoader.NewDefaultSoundFontLoader(_settings);
            soundfontLoader.SetCallbacks(_soundfontLoaderCallbacks);

            var renderer = new MidiRenderer(_settings, soundfontLoader);

            // Since the last loaded soundfont takes priority, we load the fallback soundfont first.
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
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if(File.Exists(OSXSoundfont) && SoundFont.IsSoundFont(OSXSoundfont))
                    renderer.LoadSoundfont(OSXSoundfont, true);
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if(File.Exists(WindowsSoundfont) && SoundFont.IsSoundFont(WindowsSoundfont))
                    renderer.LoadSoundfont(WindowsSoundfont, true);
            }

            lock (_renderers)
                _renderers.Add(renderer);

            return renderer;
        }

        public void FrameUpdate(float frameTime)
        {
            // Update positions of streams every frame.
            lock (_renderers)
                for (var i = 0; i < _renderers.Count; i++)
                {
                    var renderer = _renderers[i];
                    if (!renderer.Mono)
                    {
                        renderer.Source.SetGlobal();
                        continue;
                    }

                    if (renderer.TrackingCoordinates != null)
                    {
                        renderer.Source.SetPosition(renderer.TrackingCoordinates.Value.ToMapPos(_mapManager));
                    }
                    else if (renderer.TrackingEntity != null)
                    {
                        renderer.Source.SetPosition(renderer.TrackingEntity.Transform.WorldPosition);
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
                lock(_renderers)
                    for (var i = 0; i < _renderers.Count; i++)
                    {
                        var renderer = _renderers[i];
                        if(renderer != null && !renderer.Disposed)
                            renderer.Render();
                        else
                            _renderers.RemoveAt(i);
                    }

                Thread.Sleep(1);
            }
        }

        public void Dispose()
        {
            _alive = false;
            _midiThread?.Join();
            _settings?.Dispose();
            foreach (var renderer in _renderers)
            {
                renderer?.Dispose();
            }
        }

        /// <summary>
        ///     This class is used to load soundfonts.
        /// </summary>
        private class ResourceLoaderCallbacks : SoundFontLoaderCallbacks
        {
            private readonly Dictionary<int, Stream> _openStreams = new Dictionary<int, Stream>();
            private int _nextStreamId = 1;

            public override IntPtr Open(string filename)
            {
                Stream stream;
                if (filename.StartsWith("/Resources/"))
                {
                    if (!IoCManager.Resolve<IResourceCache>().TryContentFileRead(filename.Substring(10), out stream))
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

                byte[] buffer;
                try
                {
                    buffer = stream.ReadExact(length);
                }
                catch (EndOfStreamException)
                {
                    return -1;
                }
                buffer.CopyTo(span);
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
