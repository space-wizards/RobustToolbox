using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Commons.Music.Midi;
using NFluidsynth;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using MidiEvent = Commons.Music.Midi.MidiEvent;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Robust.Client.Audio.Midi
{
    public interface IMidiManager
    {
        IEnumerable<(string Id, string Name)> Inputs { get; }
        IMidiRenderer GetNewRenderer();
    }

    public class MidiManager : IPostInjectInit, IDisposable, IMidiManager
    {
#pragma warning disable 169
        [Dependency] private IClydeAudio _clydeAudio;
#pragma warning enable 169

        private bool _alive = true;
        private Settings _settings;
        private IMidiAccess2 _access;
        private List<MidiRenderer> _renderers = new List<MidiRenderer>();
        private Thread _midiThread;

        public IEnumerable<(string Id, string Name)> Inputs
        {
            get
            {
                var inputIds = new List<(string, string)>();
                foreach (var input in _access.Inputs)
                {
                    inputIds.Add((input.Id, input.Name));
                }

                return inputIds;
            }
        }

        public void PostInject()
        {
            _settings = new Settings();

            _settings["synth.sample-rate"].DoubleValue = 48000;
            _settings["player.timing-source"].StringValue = "sample";
            _settings["synth.lock-memory"].IntValue = 0;
            _settings["audio.driver"].StringValue = "file";

            _access = (IMidiAccess2) MidiAccessManager.Default;
            _midiThread = new Thread(ThreadUpdate);
            _midiThread.Start();
        }

        public IMidiRenderer GetNewRenderer()
        {
            var renderer = new MidiRenderer(_settings, _access);
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
                    renderer?.Render();
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
