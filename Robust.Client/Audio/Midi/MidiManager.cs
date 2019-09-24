using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Commons.Music.Midi;
using NFluidsynth;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Reflection;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using MidiEvent = Commons.Music.Midi.MidiEvent;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Robust.Client.Audio.Midi
{
    public interface IMidiManager
    {
        IMidiRenderer GetNewRenderer();
    }

    public class MidiManager : IPostInjectInit, IDisposable, IMidiManager
    {
#pragma warning disable 169
        [Dependency] private IClydeAudio _clydeAudio;
#pragma warning enable 169

        private bool _alive = true;
        private Settings _settings;
        private SoundFontLoader _soundfontLoader;
        private List<MidiRenderer> _renderers = new List<MidiRenderer>();
        private Thread _midiThread;

        public void PostInject()
        {
            _settings = new Settings();

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

        public IMidiRenderer GetNewRenderer()
        {
            var renderer = new MidiRenderer(_settings, _soundfontLoader);
            _renderers.Add(renderer);

            return renderer;
        }

        public void ThreadUpdate()
        {
            while (_alive)
            {
                for (var i = 0; i < _renderers.Count; i++)
                {
                    var renderer = _renderers[i];
                    if(renderer != null && !renderer.Rendering)
                        renderer.Render();
                }

                Thread.Sleep(1);
            }
        }

        public void Dispose()
        {
            _alive = false;
            _settings?.Dispose();
            foreach (var renderer in _renderers)
            {
                renderer?.Dispose();
            }
        }
    }
}
