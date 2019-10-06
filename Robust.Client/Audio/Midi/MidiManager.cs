using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using NFluidsynth;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.IoC;

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
    }

    internal class MidiManager : IPostInjectInit, IDisposable, IMidiManager
    {
        private bool _alive = true;
        private Settings _settings;
        private SoundFontLoader _soundFontLoader;
        private List<MidiRenderer> _renderers = new List<MidiRenderer>();
        private Thread _midiThread;

        private static readonly string[] LinuxSoundfonts =
        {
            "/usr/share/soundfonts/default.sf2",
            "/usr/share/soundfonts/FluidR3_GM.sf2",
            "/usr/share/soundfonts/freepats-general-midi.sf2",
            "/usr/share/sounds/sf2/FluidR3_GM.sf2",
            "/usr/share/sounds/sf2/TimGM6mb.sf2",
            "/usr/share/sounds/sf2/FluidR3_GS.sf2",
        };

        private static readonly string WindowsSoundfont = "c:\\WINDOWS\\system32\\drivers\\gm.dls";

        private static readonly string OSXSoundfont =
            "/System/Library/Components/CoreAudio.component/Contents/Resources/gs_instruments.dls";

        private static readonly string FallbackSoundfont = "/Resources/Midi/fallback.sf2";

        public void PostInject()
        {
            _settings = new Settings();
            _soundFontLoader = SoundFontLoader.NewDefaultSoundFontLoader(_settings);
            _soundFontLoader.SetCallbacks(new ResourceLoaderCallbacks());
            _settings["synth.sample-rate"].DoubleValue = 48000;
            _settings["player.timing-source"].StringValue = "sample";
            _settings["synth.lock-memory"].IntValue = 0;
            _settings["synth.threadsafe-api"].IntValue = 1;
            _settings["audio.driver"].StringValue = "file";
            _settings["midi.autoconnect"].IntValue = 1;
            _settings["player.reset-synth"].IntValue = 0;

            _midiThread = new Thread(ThreadUpdate);
            _midiThread.Start();
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
            var renderer = new MidiRenderer(_settings, _soundFontLoader);

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
                    catch (Exception e)
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
                        renderer?.Render();
                    }

                Thread.Sleep(1);
            }
        }

        public void Dispose()
        {
            _alive = false;
            _midiThread.Join();
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
            private BinaryReader _file;
            private byte[] _fileData;

            public override unsafe IntPtr Open(string filename)
            {
                Stream stream;
                if (filename.StartsWith("/Resources/"))
                {
                    if (!IoCManager.Resolve<IResourceCache>().TryContentFileRead(filename.Substring(10), out stream))
                        return IntPtr.Zero;
                } else if (File.Exists(filename))
                {
                    stream = File.OpenRead(filename);
                }
                else
                {
                    return IntPtr.Zero;
                }

                _file = new BinaryReader(stream);
                _fileData = _file.ReadBytes((int) stream.Length);
                _file.BaseStream.Position = 0;
                fixed (byte* ptr = _fileData)
                {
                    return (IntPtr) ptr;
                }
            }

            public override unsafe int Read(IntPtr buf, long count, IntPtr sfHandle)
            {
                var length = (int)count;
                var span = new Span<byte>(buf.ToPointer(), length);
                _file.ReadBytes((int)count).CopyTo(span);
                return 0;
            }

            public override int Seek(IntPtr sfHandle, int offset, SeekOrigin origin)
            {
                _file.BaseStream.Seek(offset, origin);
                return 0;
            }

            public override int Tell(IntPtr sfHandle)
            {
                return (int) _file.BaseStream.Position;
            }

            public override int Close(IntPtr sfHandle)
            {
                _file.BaseStream.Close();
                _file.Close();
                _file = null;
                _fileData = null;
                return 0;
            }
        }
    }
}
