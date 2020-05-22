using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using NFluidsynth;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Asynchronous;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timers;
using Robust.Shared.Utility;
using Logger = Robust.Shared.Log.Logger;
using MidiEvent = NFluidsynth.MidiEvent;

namespace Robust.Client.Audio.Midi
{
    public enum MidiRendererStatus
    {
        None,
        Input,
        File,
    }

    public interface IMidiRenderer : IDisposable
    {
        /// <summary>
        ///     This controls whether the midi file being played will loop or not.
        /// </summary>
        bool LoopMidi { get; set; }

        /// <summary>
        ///     The midi program (instrument) the renderer is using.
        /// </summary>
        byte MidiProgram { get; set; }

        /// <summary>
        ///     The current status of the renderer.
        ///     "None" if the renderer isn't playing from input or a midi file.
        ///     "Input" if the renderer is playing from midi input.
        ///     "File" if the renderer is playing from a midi file.
        /// </summary>
        MidiRendererStatus Status { get; }

        /// <summary>
        ///     Whether the sound will play in stereo or mono.
        /// </summary>
        bool Mono { get; set; }

        /// <summary>
        ///     Whether to drop messages on the percussion channel.
        /// </summary>
        bool DisablePercussionChannel { get; set; }

        /// <summary>
        ///     Gets the total number of ticks possible for the MIDI player.
        /// </summary>
        int PlayerTotalTick { get; }

        /// <summary>
        ///     Gets the current tick of the MIDI player.
        /// </summary>
        int PlayerTick { get; }

        /// <summary>
        ///     Gets the current tick of the sequencer.
        /// </summary>
        uint SequencerTick { get; }

        /// <summary>
        ///     Gets or sets the Time Scale of the sequencer in ticks per second. Default is 1000 for 1 tick per millisecond.
        /// </summary>
        double SequencerTimeScale { get; set; }

        /// <summary>
        ///     Start listening for midi input.
        /// </summary>
        bool OpenInput();

        /// <summary>
        ///     Start playing a midi file.
        /// </summary>
        /// <param name="filename">Path to the midi file</param>
        bool OpenMidi(string filename);

        /// <summary>
        ///     Start playing a midi file.
        /// </summary>
        /// <param name="buffer">Bytes of the midi file</param>
        bool OpenMidi(ReadOnlySpan<byte> buffer);

        /// <summary>
        ///     Stops listening for midi input.
        /// </summary>
        bool CloseInput();

        /// <summary>
        ///     Stops playing midi files.
        /// </summary>
        bool CloseMidi();

        /// <summary>
        ///     Stops all notes being played currently.
        /// </summary>
        void StopAllNotes();

        /// <summary>
        ///     Loads a new soundfont into the renderer.
        /// </summary>
        void LoadSoundfont(string filename, bool resetPresets = false);

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
        IEntity TrackingEntity { get; set; }

        /// <summary>
        ///     The position that will be used for positional audio.
        ///     This is only used if <see cref="Mono"/> is set to True
        ///     and <see cref="TrackingEntity"/> is null.
        /// </summary>
        GridCoordinates? TrackingCoordinates { get; set; }

        /// <summary>
        ///     Send a midi event for the renderer to play.
        /// </summary>
        /// <param name="midiEvent">The midi event to be played</param>
        void SendMidiEvent(Shared.Audio.Midi.MidiEvent midiEvent);

        /// <summary>
        ///     Schedule a MIDI event to be played at a later time.
        /// </summary>
        /// <remarks>Does NOT raise a <see cref="OnMidiEvent"/> as the event is sent directly to the <see cref="Synth"/> using a <see cref="Sequencer"/>.</remarks>
        /// <param name="midiEvent">the midi event in question</param>
        /// <param name="time"></param>
        /// <param name="absolute"></param>
        void ScheduleMidiEvent(Shared.Audio.Midi.MidiEvent midiEvent, uint time, bool absolute);
    }

    public class MidiRenderer : IMidiRenderer
    {
#pragma warning disable 649
        [Dependency] private IClydeAudio _clydeAudio;
        [Dependency] private ITaskManager _taskManager;
        [Dependency] private ILogManager _logger;
#pragma warning restore 649
        
        private const int MidiSizeLimit = 2000000;
        private const double BytesToMegabytes = 0.000001d;

        private readonly ISawmill _midiSawmill;

        private Settings _settings;

        // Kept around to avoid the loader callbacks getting GC'd
        // ReSharper disable once NotAccessedField.Local
        private readonly SoundFontLoader _soundFontLoader;
        private Synth _synth;
        private Sequencer _sequencer;
        private NFluidsynth.Player _player;
        private MidiDriver _driver;
        private byte _midiprogram = 1;
        private bool _loopMidi = false;
        private const int SampleRate = 44100;
        private const int Buffers = SampleRate / 2205;
        private readonly object _playerStateLock = new object();
        private SequencerClientId _synthRegister;
        public IClydeBufferedAudioSource Source { get; set; }
        public bool Disposed { get; private set; } = false;

        public byte MidiProgram
        {
            get => _midiprogram;
            set
            {
                lock (_playerStateLock)
                    for (var i = 0; i < 16; i++)
                        _synth.ProgramChange(i, value);

                _midiprogram = value;
            }
        }

        public bool DisablePercussionChannel { get; set; } = true;

        public int PlayerTotalTick => _player?.GetTotalTicks ?? 0;
        public int PlayerTick => _player?.CurrentTick ?? 0;
        public uint SequencerTick => _sequencer?.Tick ?? 0;

        public double SequencerTimeScale
        {
            get => _sequencer.TimeScale;
            set => _sequencer.TimeScale = value;
        }

        public bool Mono { get; set; }
        public MidiRendererStatus Status { get; private set; } = MidiRendererStatus.None;

        public bool LoopMidi
        {
            get => _loopMidi;
            set
            {
                lock (_playerStateLock)
                    _player?.SetLoop(value ? -1 : 1);
                _loopMidi = value;
            }
        }

        public IEntity TrackingEntity { get; set; } = null;
        public GridCoordinates? TrackingCoordinates { get; set; } = null;

        internal bool Free { get; set; } = false;

        internal MidiRenderer(Settings settings, SoundFontLoader soundFontLoader, bool mono = true)
        {
            IoCManager.InjectDependencies(this);
            _midiSawmill = _logger.GetSawmill("midi");
            Source = _clydeAudio.CreateBufferedAudioSource(Buffers, true);
            Source.SampleRate = SampleRate;
            _settings = settings;
            _soundFontLoader = soundFontLoader;
            _synth = new Synth(_settings);
            _sequencer = new Sequencer(false);
            _synthRegister = _sequencer.RegisterFluidsynth(_synth);

            _synth.AddSoundFontLoader(soundFontLoader);
            Mono = mono;
            Source.EmptyBuffers();
            Source.StartPlaying();
        }

        public bool OpenInput()
        {
            if (Disposed)
                return false;

            if (Status != MidiRendererStatus.File) CloseMidi();
            Status = MidiRendererStatus.Input;
            StopAllNotes();

            _driver = new MidiDriver(_settings, MidiDriverEventHandler);
            return true;
        }

        public bool OpenMidi(string filename)
        {
            return OpenMidi(File.ReadAllBytes(filename));
        }

        public bool OpenMidi(ReadOnlySpan<byte> buffer)
        {
            if (Disposed)
                return false;

            if (Status == MidiRendererStatus.Input) CloseInput();
            Status = MidiRendererStatus.File;
            StopAllNotes();

            if (buffer.Length > MidiSizeLimit)
            {
                _midiSawmill.Error("Midi file selected is too big! It was {0} MB but it should be less than {1} MB.",
                    buffer.Length * BytesToMegabytes, MidiSizeLimit * BytesToMegabytes);
                CloseMidi();
                return false;
            }

            lock (_playerStateLock)
            {
                if (_player == null)
                    _player = new NFluidsynth.Player(_synth);
                _player.Stop();
                _player.AddMem(buffer);
                _player.SetPlaybackCallback(MidiPlayerEventHandler);
                _player.Play();
                _player.SetLoop(LoopMidi ? -1 : 1);
            }

            return true;
        }

        public bool CloseInput()
        {
            if (Status != MidiRendererStatus.Input) return false;
            Status = MidiRendererStatus.None;
            _driver?.Dispose();
            _driver = null;
            StopAllNotes();
            return true;
        }

        public bool CloseMidi()
        {
            if (Status != MidiRendererStatus.File) return false;
            Status = MidiRendererStatus.None;
            lock (_playerStateLock)
            {
                if (_player == null) return false;
                _player?.Stop();
                _player?.Dispose();
                _player = null;
            }

            StopAllNotes();
            return true;
        }

        public void StopAllNotes()
        {
            for (var i = 0; i < 16; i++)
            {
                _synth.AllNotesOff(i);
            }
        }

        public void LoadSoundfont(string filename, bool resetPresets = false)
        {
            lock (_playerStateLock)
            {
                _synth.LoadSoundFont(filename, resetPresets);
                for (var i = 0; i < 16; i++)
                    _synth.SoundFontSelect(i, 1);
            }
        }

        public event Action<Shared.Audio.Midi.MidiEvent> OnMidiEvent;
        public event Action OnMidiPlayerFinished;

        internal void Render(int length = SampleRate / 250)
        {
            if (Disposed) return;

            // SSE needs this.
            DebugTools.Assert(length % 4 == 0, "Sample length must be multiple of 4");

            var buffersProcessed = Source.GetNumberOfBuffersProcessed();
            if(buffersProcessed == Buffers) _midiSawmill.Warning("MIDI buffer overflow!");
            if (buffersProcessed == 0) return;

            var bufferLength = length * 2;

            unsafe
            {
                Span<uint> buffers = stackalloc uint[buffersProcessed];
                Span<float> audio = stackalloc float[bufferLength * buffers.Length];

                Source.GetBuffersProcessed(buffers);

                lock (_playerStateLock)
                    _synth?.WriteSampleFloat(length * buffers.Length, audio, 0, Mono ? 1 : 2,
                        audio, Mono ? length * buffers.Length : 1, Mono ? 1 : 2);

                if (Mono) // Turn audio to mono
                {
                    var l = length * buffers.Length;

                    if (Sse.IsSupported)
                    {
                        fixed (float* ptr = audio)
                        {
                            for (var j = 0; j < l; j += 4)
                            {
                                var k = j + l;

                                var jV = Sse.LoadVector128(ptr + j);
                                var kV = Sse.LoadVector128(ptr + k);

                                Sse.Store(j + ptr, Sse.Add(jV, kV));
                            }
                        }
                    }
                    else
                    {
                        for (var j = 0; j < l; j++)
                        {
                            var k = j + l;
                            audio[j] = ((audio[k] + audio[j]));
                        }
                    }
                }

                for (var i = 0; i < buffers.Length; i++)
                {
                    var buffer = buffers[i];
                    Source.WriteBuffer(buffer, audio.Slice(i * length, bufferLength));
                }

                Source.QueueBuffers(buffers);
            }

            lock (_playerStateLock)
            {
                if (Status == MidiRendererStatus.File && _player?.Status == FluidPlayerStatus.Done)
                {
                    _taskManager.RunOnMainThread(() => OnMidiPlayerFinished?.Invoke());
                    CloseMidi();
                }
            }

            if (!Source.IsPlaying) Source.StartPlaying();
        }

        private int MidiPlayerEventHandler(MidiEvent midiEvent)
        {
            if (Disposed || Status != MidiRendererStatus.File && _player?.Status == FluidPlayerStatus.Playing) return 0;
            var timestamp = SequencerTick;
            var midiEv = (Shared.Audio.Midi.MidiEvent) midiEvent;
            midiEv.Timestamp = timestamp;
            SendMidiEvent(midiEv);
            return 0;
        }

        private int MidiDriverEventHandler(MidiEvent midiEvent)
        {
            if (Disposed || Status != MidiRendererStatus.Input) return 0;
            var timestamp = SequencerTick;
            var midiEv = (Shared.Audio.Midi.MidiEvent) midiEvent;
            midiEv.Timestamp = timestamp;
            SendMidiEvent(midiEv);
            return 0;
        }

        public void SendMidiEvent(Shared.Audio.Midi.MidiEvent midiEvent)
        {
            if (Disposed) return;

            if (DisablePercussionChannel && midiEvent.Channel == 9)
                return;

            // We play every note on channel 0 to prevent a bug where some notes didn't get turned off correctly.
            const int ch = 0;

            try
            {
                lock(_playerStateLock)
                    if (midiEvent.Type == 144 && midiEvent.Velocity != 0)
                        _synth.NoteOn(midiEvent.Channel, midiEvent.Key, midiEvent.Velocity);
                    else if (midiEvent.Type == 128 || (midiEvent.Type == 144 && midiEvent.Velocity == 0))
                        _synth.NoteOff(midiEvent.Channel, midiEvent.Key);
                    else if (midiEvent.Type == 224)
                        _synth.PitchBend(midiEvent.Channel, midiEvent.Pitch);
                    else if (midiEvent.Type == 176)
                        _synth.CC(midiEvent.Channel, midiEvent.Control, midiEvent.Value);
            }
            catch (FluidSynthInteropException e)
            {
                _midiSawmill.Error("Exception while sending midi event of type {0}: {1}", midiEvent.Type, e);
            }

            _taskManager.RunOnMainThread(() => OnMidiEvent?.Invoke(midiEvent));
        }

        public void ScheduleMidiEvent(Shared.Audio.Midi.MidiEvent midiEvent, uint time, bool absolute = false)
        {
            if (Disposed) return;

            var seqEv = (SequencerEvent) midiEvent;
            seqEv.Dest = _synthRegister;
            _sequencer.SendAt(seqEv, time, absolute);
        }

        public void Dispose()
        {
            Disposed = true;

            switch (Status)
            {
                case MidiRendererStatus.Input:
                    CloseInput();
                    break;
                case MidiRendererStatus.File:
                    CloseMidi();
                    break;
            }

            Source?.Dispose();
            _synth?.Dispose();
            _player?.Dispose();
            _driver?.Dispose();
            _sequencer?.Dispose();

            _settings = null;
            Source = null;
            _synth = null;
            _player = null;
            _driver = null;
            _sequencer = null;
        }
    }
}
