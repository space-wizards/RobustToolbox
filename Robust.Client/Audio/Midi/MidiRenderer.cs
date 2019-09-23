using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NFluidsynth;
using Commons.Music.Midi;
using Commons.Music.Midi.Alsa;
using Namotion.Reflection;
using OpenTK.Audio.OpenAL;
using Robust.Client.Graphics;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Logger = Robust.Shared.Log.Logger;
using MidiEvent = NFluidsynth.MidiEvent;

namespace Robust.Client.Audio.Midi
{
    public interface IMidiRenderer : IDisposable
    {
        IReadOnlyCollection<int> NotesPlaying { get; }
        int MidiProgram { set; }
        bool IsInputOpen { get; }
        bool IsMidiOpen { get; }
        bool Mono { get; set; }
        void OpenInput();
        void OpenMidi(string filename);
        void OpenMidi(Stream stream);
        void CloseInput();
        void CloseMidi();
        void LoadSoundfont(string filename);
        event Action<Shared.Audio.Midi.MidiEvent> OnMidiEvent;
        event Action OnMidiPlayerFinished;
        IEntity Position { get; set; }
        void SendMidiEvent(MidiEvent midiEvent);
    }

    public class MidiRenderer : IMidiRenderer
    {
        private Settings _settings;
        private Synth _synth;
        private NFluidsynth.Player _player;
        private MidiDriver _driver;
        private List<int> _notesPlaying = new List<int>();
        private int _source;
        private int[] _buffers;
        private const int SampleRate = 48000;
        private const int Buffers = SampleRate/1000;
        public IReadOnlyCollection<int> NotesPlaying => _notesPlaying;

        public int MidiProgram
        {
            set
            {
                for (var i = 0; i < 16; i++)
                    _synth.ProgramChange(i, value);
            }
        }

        public bool IsInputOpen => _driver != null;
        public bool IsMidiOpen => _player != null;
        public bool Mono { get; set; } = true;
        public bool Rendering { get; set; } = false;
        public IEntity Position { get; set; } = null;

        internal bool Free { get; set; } = false;
        internal bool NeedsRendering { get; private set; } = false;

        internal MidiRenderer(Settings settings, SoundFontLoader soundfont)
        {
            _settings = settings;
            _synth = new Synth(_settings);
            _source = AL.GenSource();
            _buffers = AL.GenBuffers(Buffers);
            EmptyBuffers();
            AL.SourcePlay(_source);
        }

        private unsafe void EmptyBuffers()
        {
            var length = (SampleRate / Buffers) * (Mono ? 1 : 2);

            for (var i = 0; i < Buffers; i++)
            {
                var empty = new Span<ushort>(new ushort[length]);
                fixed (ushort* ptr = empty)
                {
                    AL.BufferData(_buffers[i], Mono ? ALFormat.Mono16 : ALFormat.Stereo16, (IntPtr) ptr, length, SampleRate);
                    AL.SourceQueueBuffers(_source, 1, new []{_buffers[i]});
                }
            }
        }

        public void OpenInput()
        {
            _driver = new MidiDriver(_settings, MidiDriverEventHandler);
        }

        public void OpenMidi(string filename)
        {
            _player = new NFluidsynth.Player(_synth);
            _player.Add(filename);
            _player.SetPlaybackCallback(MidiPlayerEventHandler);
            _player.Play();
        }

        public void OpenMidi(Stream stream)
        {
            throw new NotImplementedException();
        }

        public void CloseInput()
        {
            _driver?.Dispose();
            _driver = null;
        }

        public void CloseMidi()
        {
            _player?.Stop();
            _player?.Dispose();
            _player = null;
        }

        public void LoadSoundfont(string filename)
        {
            _synth.LoadSoundFont(filename, false);
            for (var i = 0; i < 16; i++)
                _synth.SoundFontSelect(i, 0);
            MidiProgram = 1;
        }

        public event Action<Shared.Audio.Midi.MidiEvent> OnMidiEvent;
        public event Action OnMidiPlayerFinished;

        internal void Render(int length = SampleRate/1000)
        {
            var status = AL.GetSourceState(_source);
            if(Position != null && Mono)
                AL.Source(_source, ALSource3f.Position, Position.Transform.GridPosition.X, Position.Transform.GridPosition.Y, 0f);
            AL.GetSource(_source, ALGetSourcei.BuffersProcessed, out var buffersProcessed);
            if (buffersProcessed == 0) return;

            Rendering = true;

            unsafe
            {
                var buffers = AL.SourceUnqueueBuffers(_source, buffersProcessed);

                for (var i = 0; i < buffers.Length; i++)
                {
                    var buffer = buffers[i];

                    ushort[] left;
                    if (Mono)
                    {
                        left = new ushort[length];
                        var right = new ushort[length];
                        _synth?.WriteSample16( left, 0, 1, right, 0, 1);
                        left = left.Zip(right, (x, y) => (ushort) (x + y)).ToArray();
                    }
                    else
                    {
                        left = new ushort[length*2];
                        _synth?.WriteSample16( left, 0, 1, left, 1, 2);
                    }

                    fixed (ushort* ptr = left)
                    {
                        AL.BufferData(buffer, Mono ? ALFormat.Mono16 : ALFormat.Stereo16, (IntPtr) ptr, Mono ? length * sizeof(ushort) : length*sizeof(ushort)*2, SampleRate);
                    }
                }

                AL.SourceQueueBuffers(_source, buffersProcessed, buffers);
            }

            if (IsMidiOpen && _player.Status == FluidPlayerStatus.Done)
            {
                OnMidiPlayerFinished?.Invoke();
                CloseMidi();
            }

            if(status != ALSourceState.Playing) AL.SourcePlay(_source);

            Rendering = false;
        }

        private int MidiPlayerEventHandler(MidiEvent evt)
        {
            if (IsMidiOpen == false) return 0;
            SendMidiEvent(evt);
            System.Console.WriteLine(evt.Type);
            return 0;
        }

        private int MidiDriverEventHandler(MidiEvent midiEvent)
        {
            if (IsInputOpen == false) return 0;
            SendMidiEvent(midiEvent);
            return 0;
        }

        public void SendMidiEvent(MidiEvent midiEvent)
        {
            var ch = midiEvent.Channel;

            try
            {
                switch (midiEvent.Type)
                {
                    case 128:
                        if (_notesPlaying.Contains(midiEvent.Key))
                        {
                            _synth.NoteOff(ch, midiEvent.Key);
                            _notesPlaying.Remove(midiEvent.Key);
                        }

                        break;
                    case 144:
                        if (midiEvent.Velocity == 0)
                        {
                            if (_notesPlaying.Contains(midiEvent.Key))
                            {
                                _synth.NoteOff(ch, midiEvent.Key);
                                _notesPlaying.Remove(midiEvent.Key);
                            }
                        }
                        else
                        {
                            _synth.NoteOn(ch, midiEvent.Key, midiEvent.Velocity);
                            if (!_notesPlaying.Contains(midiEvent.Key))
                                _notesPlaying.Add(midiEvent.Key);
                        }

                        break;
                    default:
                        break;
                }
            }
            catch (FluidSynthInteropException interopException)
            { }

            OnMidiEvent?.Invoke((Shared.Audio.Midi.MidiEvent) midiEvent);

            NeedsRendering = true;
        }

        public void Dispose()
        {
            if(IsInputOpen) CloseInput();
            if(IsMidiOpen) CloseMidi();

            /*_synth?.Dispose();
            _player?.Dispose();
            _driver?.Dispose();*/

            _settings = null;
            _synth = null;
            _player = null;
            _driver = null;
        }
    }
}
