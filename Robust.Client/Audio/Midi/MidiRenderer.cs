using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NFluidsynth;
using OpenTK.Audio.OpenAL;
using Robust.Shared.Interfaces.GameObjects;
using MidiEvent = NFluidsynth.MidiEvent;
using Logger = Robust.Shared.Log.Logger;

namespace Robust.Client.Audio.Midi
{
    public interface IMidiRenderer : IDisposable
    {
        /// <summary>
        ///     This controls whether the midi file being played will loop or not.
        /// </summary>
        bool LoopMidi { get; set; }

        /// <summary>
        ///     This is a collection of notes currently being played.
        /// </summary>
        IReadOnlyCollection<int> NotesPlaying { get; }

        /// <summary>
        ///     The midi program (instrument) the renderer is using.
        /// </summary>
        int MidiProgram { get; set; }

        /// <summary>
        ///     Whether the renderer is listening for midi input or not.
        /// </summary>
        bool IsInputOpen { get; }

        /// <summary>
        ///     Whether a midi file is being played or not.
        /// </summary>
        bool IsMidiOpen { get; }

        /// <summary>
        ///     Whether the sound will play in stereo or mono.
        /// </summary>
        bool Mono { get; set; }

        /// <summary>
        ///     Start listening for midi input.
        /// </summary>
        void OpenInput();

        /// <summary>
        ///     Start playing a midi file.
        /// </summary>
        /// <param name="filename">Path to the midi file</param>
        void OpenMidi(string filename);

        /// <summary>
        ///     Start playing a midi file.
        /// </summary>
        /// <param name="stream">Bytes of the midi file</param>
        void OpenMidi(Span<byte> stream);

        /// <summary>
        ///     Stops listening for midi input.
        /// </summary>
        void CloseInput();

        /// <summary>
        ///     Stops playing midi files.
        /// </summary>
        void CloseMidi();

        /// <summary>
        ///     Stops all notes being played currently.
        /// </summary>
        void StopAllNotes();

        /// <summary>
        ///     Loads a new soundfont into the renderer.
        /// </summary>
        void LoadSoundfont(string filename, bool resetPresets);

        /// <summary>
        ///     Invoked whenever a new midi event is registered.
        /// </summary>
        event Action<Shared.Audio.Midi.MidiEvent> OnMidiEvent;

        /// <summary>
        ///     Invoked when the midi player finishes playing a song.
        /// </summary>
        event Action OnMidiPlayerFinished;

        /// <summary>
        ///     The entity whose position will be used for positional audio.
        ///     This is only used if <see cref="Mono"/> is set to True.
        /// </summary>
        IEntity Position { get; set; }

        /// <summary>
        ///     Send a midi event for the renderer to play.
        /// </summary>
        /// <param name="midiEvent">The midi event to be played</param>
        void SendMidiEvent(Shared.Audio.Midi.MidiEvent midiEvent);
    }

    public class MidiRenderer : IMidiRenderer
    {
        private const int NoteLimit = 15;

        private Settings _settings;
        private Synth _synth;
        private NFluidsynth.Player _player;
        private MidiDriver _driver;
        private SoundFontLoader _soundFontLoader;
        private List<int> _notesPlaying = new List<int>();
        private int _midiprogram = 1;
        private int _source;
        private int[] _buffers;
        private bool _loopMidi = false;
        private const int SampleRate = 48000;
        private const int Buffers = SampleRate/1000;
        public IReadOnlyCollection<int> NotesPlaying => _notesPlaying;

        public int MidiProgram
        {
            get => _midiprogram;
            set
            {
                for (var i = 0; i < 16; i++)
                    _synth.ProgramChange(i, value);

                _midiprogram = value;
            }
        }

        public bool LoopMidi
        {
            get => _loopMidi;
            set
            {
                _player?.SetLoop(value ? -1 : 1);
                _loopMidi = value;
            }
        }

        public bool IsInputOpen => _driver != null;
        public bool IsMidiOpen => _player != null;
        public bool Mono { get; set; } = true;
        public IEntity Position { get; set; } = null;

        internal bool Free { get; set; } = false;

        internal MidiRenderer(Settings settings, SoundFontLoader soundFontLoader)
        {
            _settings = settings;
            _soundFontLoader = soundFontLoader;
            _synth = new Synth(_settings);
            _synth.AddSoundFontLoader(_soundFontLoader);
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
                    AL.BufferData(_buffers[i], Mono ? ALFormat.Mono16 : ALFormat.Stereo16, (IntPtr) ptr, length*sizeof(ushort), SampleRate);
                    AL.SourceQueueBuffers(_source, 1, new []{_buffers[i]});
                }
            }
        }

        public void OpenInput()
        {
            StopAllNotes();

            _driver = new MidiDriver(_settings, MidiDriverEventHandler);
        }

        public void OpenMidi(string filename)
        {
            OpenMidi(File.ReadAllBytes(filename));
        }

        public void OpenMidi(Span<byte> buffer)
        {
            StopAllNotes();

            if(_player == null)
                _player = new NFluidsynth.Player(_synth);

            lock (_player)
            {
                _player.Stop();
                _player.AddMem(buffer);
                _player.SetPlaybackCallback(MidiPlayerEventHandler);
                _player.Play();
                _player.SetLoop(LoopMidi ? -1 : 1);
            }
        }

        public void CloseInput()
        {
            _driver?.Dispose();
            _driver = null;
            StopAllNotes();
        }

        public void CloseMidi()
        {
            if (_player == null) return;
            lock (_player)
            {
                _player?.Stop();
                _player?.Dispose();
                _player = null;
            }

            StopAllNotes();
        }

        public void StopAllNotes()
        {
            foreach (var note in _notesPlaying.ToArray())
            {
                SendMidiEvent(new Shared.Audio.Midi.MidiEvent(){Type = 128, Key = note});
            }
        }

        public void LoadSoundfont(string filename, bool resetPresets = false)
        {
            _synth.LoadSoundFont(filename, resetPresets);
            for (var i = 0; i < 16; i++)
                _synth.SoundFontSelect(i, 1);
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

        }

        private int MidiPlayerEventHandler(MidiEvent midiEvent)
        {
            if (IsMidiOpen == false) return 0;
            SendMidiEvent((Shared.Audio.Midi.MidiEvent) midiEvent);
            return 0;
        }

        private int MidiDriverEventHandler(MidiEvent midiEvent)
        {
            if (IsInputOpen == false) return 0;
            SendMidiEvent((Shared.Audio.Midi.MidiEvent) midiEvent);
            return 0;
        }

        public void SendMidiEvent(Shared.Audio.Midi.MidiEvent midiEvent)
        {
            // We play every note on channel 0 to prevent a bug where some notes didn't get turned off correctly.
            const int ch = 0;

            try
            {
                switch (midiEvent.Type)
                {
                    // NoteOff
                    case 128:
                        if (_notesPlaying.Contains(midiEvent.Key))
                        {
                            _synth.NoteOff(ch, midiEvent.Key);
                            _notesPlaying.Remove(midiEvent.Key);
                        }

                        break;
                    // NoteOn
                    case 144:
                        // NoteOn with 0 velocity is the same as NoteOff
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
                            // If we're at the limit of notes at once, we drop this one.
                            if (_notesPlaying.Count >= NoteLimit)
                                return;

                            _synth.NoteOn(ch, midiEvent.Key, midiEvent.Velocity);
                            if (!_notesPlaying.Contains(midiEvent.Key))
                                _notesPlaying.Add(midiEvent.Key);
                        }

                        break;
                    default:
                        if (_notesPlaying.Contains(midiEvent.Key))
                        {
                            _synth.NoteOff(ch, midiEvent.Key);
                            _notesPlaying.Remove(midiEvent.Key);
                        }
                        break;
                }
            }
            catch (FluidSynthInteropException interopException)
            { }

            OnMidiEvent?.Invoke(midiEvent);
        }

        public void Dispose()
        {
            if(IsInputOpen) CloseInput();
            if(IsMidiOpen) CloseMidi();

            AL.DeleteBuffers(_buffers);
            AL.DeleteSource(_source);

            _synth?.Dispose();
            _player?.Dispose();
            _driver?.Dispose();

            _settings = null;
            _synth = null;
            _player = null;
            _driver = null;
            _soundFontLoader = null;
        }
    }
}
