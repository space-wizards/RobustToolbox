using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NFluidsynth;
using Commons.Music.Midi;
using Commons.Music.Midi.Alsa;
using Namotion.Reflection;
using NFluidsynth.MidiManager;
using Robust.Shared.Audio.Midi;
using Logger = Robust.Shared.Log.Logger;
using MidiEvent = Robust.Shared.Audio.Midi.MidiEvent;

namespace Robust.Client.Audio.Midi
{
    public interface IMidiRenderer
    {
        IReadOnlyCollection<int> NotesPlaying { get; }
        string InputId { get; }
        int MidiProgram { set; }
        bool IsInputOpen { get; }
        bool IsMidiOpen { get; }
        bool Mono { get; set; }
        void OpenInput(string id);
        void OpenMidi(Stream stream);
        void CloseInput();
        void CloseMidi();
        void LoadSoundfont(string filename);
        event Action<(ushort[] left, ushort[] right)> OnSampleRendered;

        void SendMidiEvent(MidiEvent midiEvent);
    }

    public class MidiRenderer : IDisposable, IMidiRenderer
    {
        private Settings _settings;
        private Synth _synth;
        private MidiPlayer _player;
        private IMidiAccess _access;
        private MidiDriver _driver;
        private List<int> _notesPlaying = new List<int>();

        private IMidiInput _input;
        public IReadOnlyCollection<int> NotesPlaying => _notesPlaying;
        public string InputId => _input?.Details.Id;

        public int MidiProgram
        {
            set
            {
                for (int i = 0; i < 16; i++)
                    _synth.ProgramChange(i, value);
            }
        }

        public bool IsInputOpen => _input != null;
        public bool IsMidiOpen => _player != null;
        public bool Mono { get; set; } = true;

        internal bool Free { get; set; } = false;
        internal bool NeedsRendering { get; private set; } = false;

        internal MidiRenderer(Settings settings, IMidiAccess2 access)
        {
            _settings = settings;
            _synth = new Synth(_settings);
            _access = access;
        }

        public void OpenInput(string id)
        {
            if (IsMidiOpen || IsInputOpen) return;
            _input = _access.OpenInputAsync(id).Result;
            _input.MessageReceived += InputOnEventReceived;
        }

        public void OpenMidi(Stream stream)
        {
            _player = new MidiPlayer(MidiMusic.Read(stream));
            _player.EventReceived += PlayerOnEventReceived;
            _player.Play();
            _player.Finished += CloseMidi;
        }

        public void CloseInput()
        {
            _input?.CloseAsync();
            _input = null;
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

        public event Action<(ushort[] left, ushort[] right)> OnSampleRendered;

        /// <summary>
        ///     Renders the current state of the synth into PCM.
        ///     Keep in mind, regardless of the value <see cref="Mono"/> has, this will render stereo audio.
        ///     However, <see cref="Mono"/> changes whether each channel will be outputted to different buffers (left and right)
        ///     or only one buffer for both channels (left).
        /// </summary>
        /// <returns></returns>
        internal (ushort[] left, ushort[] right) Render(int length = 44100)
        {
            if (_notesPlaying.Count == 0)
                return (new ushort[0], new ushort[0]);

            ushort[] left = null, right = null;
            if (Mono)
            {
                left = new ushort[length];
                right = new ushort[length];
                _synth.WriteSample16(length, left, 0, 1, right, 0, 1);
            }
            else
            {
                left = new ushort[length * 2];
                _synth.WriteSample16(length, left, 0, 2, left, 1, 2);
            }

            if (_notesPlaying.Count == 0)
                NeedsRendering = false;
            var t = (left, right);
            OnSampleRendered?.Invoke(t);
            return t;
        }

        public void PlayerOnEventReceived(Commons.Music.Midi.MidiEvent midiEvent)
        {
            SendMidiEvent((MidiEvent) midiEvent);
        }

        private void InputOnEventReceived(object sender, MidiReceivedEventArgs e)
        {
            System.Console.WriteLine("---");
            for (var i = 0; i < e.Length; i++)
            {
                var type = e.Data[i];
                System.Console.WriteLine(i);
                switch (type)
                {
                    case MidiEvent.NoteOffEvent:
                        SendMidiEvent(MidiEvent.NoteOff(e.Data[i+1]));
                        i += 1;
                        break;
                    case MidiEvent.NoteOnEvent:
                        SendMidiEvent(MidiEvent.NoteOn(e.Data[i + 1], e.Data[i + 2]));
                        i += 2;
                        break;
                }
            }
        }

        public void SendMidiEvent(MidiEvent midiEvent)
        {
            var ch = midiEvent.Channel;
            var msg = midiEvent.Data;

            switch (midiEvent.EventType)
            {
                case MidiEvent.NoteOffEvent:
                    if (_notesPlaying.Contains(msg[1]))
                    {
                        _synth.NoteOff(ch, msg[1]);
                        _notesPlaying.Remove(msg[1]);
                    }
                    break;
                case MidiEvent.NoteOnEvent:
                    if (msg[2] == 0)
                    {
                        if (_notesPlaying.Contains(msg[1]))
                        {
                            _synth.NoteOff(ch, msg[1]);
                            _notesPlaying.Remove(msg[1]);
                        }
                    }
                    else
                    {
                        _synth.NoteOn(ch, msg[1], msg[2]);
                        if (!_notesPlaying.Contains(msg[1]))
                            _notesPlaying.Add(msg[1]);
                    }

                    break;
                default:
                    break;
            }

            NeedsRendering = true;
        }

        public void Dispose()
        {
            _settings = null;
            _synth = null;
            _player = null;
            _input = null;
        }
    }
}
