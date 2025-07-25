using System;
using System.Collections;
using JetBrains.Annotations;
using NFluidsynth;
using Robust.Client.Graphics;
using Robust.Shared.Asynchronous;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Audio.Sources;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Audio.Midi;

internal sealed partial class MidiRenderer : IMidiRenderer
{
    private readonly IMidiManager _midiManager;
    private readonly ITaskManager _taskManager;

    // TODO: Make this a replicated CVar in MidiManager
    private const int MidiSizeLimit = 2000000;
    private const double BytesToMegabytes = 0.000001d;
    private const int ChannelCount = RobustMidiEvent.MaxChannels;

    private readonly ISawmill _midiSawmill;

    private readonly Settings _settings;

    [ViewVariables(VVAccess.ReadWrite)] private bool _debugEvents = false;

    // Kept around to avoid the loader callbacks getting GC'd
    // ReSharper disable once NotAccessedField.Local
    private readonly SoundFontLoader _soundFontLoader;
    private readonly Synth _synth;
    private readonly Sequencer _sequencer;
    private NFluidsynth.Player? _player;
    private int _playerTotalTicks;
    private MidiDriver? _driver;
    private byte _midiProgram = 1;
    private byte _midiBank = 1;
    private uint _midiSoundfont = 0;
    private bool _loopMidi = false;
    private const int SampleRate = 44100;
    private const int Buffers = SampleRate / 2205;
    private readonly object _playerStateLock = new();
    private readonly SequencerClientId _synthRegister;
    private readonly SequencerClientId _robustRegister;
    private readonly SequencerClientId _debugRegister;

    [ViewVariables] private MidiRendererState _rendererState = new();

    private IMidiRenderer? _master;
    public MidiRendererState RendererState => _rendererState;
    public IBufferedAudioSource Source { get; set; }
    IBufferedAudioSource IMidiRenderer.Source => Source;

    [ViewVariables]
    public bool Disposed { get; private set; } = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public byte MidiProgram
    {
        get => _midiProgram;
        set
        {
            var disable = DisableProgramChangeEvent;
            DisableProgramChangeEvent = false;

            lock (_playerStateLock)
            {
                for (byte i = 0; i < ChannelCount; i++)
                {
                    // Don't change percussion channel instrument.
                    if (i == RobustMidiEvent.PercussionChannel)
                        continue;

                    SendMidiEvent(RobustMidiEvent.ProgramChange(i, value, SequencerTick));
                }
            }

            DisableProgramChangeEvent = disable;
            _midiProgram = value;
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public byte MidiBank
    {
        get => _midiBank;
        set
        {
            var disable = DisableProgramChangeEvent;
            DisableProgramChangeEvent = false;

            lock (_playerStateLock)
            {
                for (byte i = 0; i < ChannelCount; i++)
                {
                    // Don't change percussion channel bank.
                    if (i == RobustMidiEvent.PercussionChannel)
                        continue;

                    SendMidiEvent(RobustMidiEvent.BankSelect(i, value, SequencerTick));

                    // Re-select program.
                    SendMidiEvent(RobustMidiEvent.ProgramChange(i, _midiProgram, SequencerTick));
                }
            }

            DisableProgramChangeEvent = disable;
            _midiBank = value;
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public uint MidiSoundfont
    {
        get => _midiSoundfont;
        set
        {
            lock (_playerStateLock)
            {
                for (var i = 0; i < _synth.MidiChannelCount; i++)
                {
                    _synth.SoundFontSelect(i, value);
                }
            }

            _midiSoundfont = value;
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public bool DisablePercussionChannel
    {
        get => FilteredChannels[RobustMidiEvent.PercussionChannel];
        set => FilteredChannels[RobustMidiEvent.PercussionChannel] = value;
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public bool DisableProgramChangeEvent { get; set; } = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public int PlayerTotalTick
    {
        get
        {
            // GetTotalTicks is really expensive (has to iterate the entire file, not cached).
            // Slight problem with caching it ourselves: the value only becomes available when the player loads the MIDI file.
            // And that only happens after playback really starts, with the timer and synth and all that stuff.
            // So we cache it "as soon as it's available", i.e. not 0.
            // We don't care about playlists and such, so it shouldn't change anymore after.
            if (_playerTotalTicks != 0)
                return _playerTotalTicks;

            return _playerTotalTicks = _player?.GetTotalTicks ?? 0;
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public int PlayerTick
    {
        get => _player?.CurrentTick ?? 0;
        set
        {
            lock (_playerStateLock)
            {
                _player?.Seek(Math.Max(Math.Min(value, PlayerTotalTick-1), 0));
            }
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public uint SequencerTick => !Disposed ? _sequencer?.Tick ?? 0 : 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public double SequencerTimeScale
    {
        get => !Disposed ? _sequencer?.TimeScale ?? 0 : 0;
        set => _sequencer.TimeScale = value;
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Mono { get; set; }

    [ViewVariables]
    public MidiRendererStatus Status { get; private set; } = MidiRendererStatus.None;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool LoopMidi
    {
        get => _loopMidi;
        set
        {
            lock (_playerStateLock)
            {
                _player?.SetLoop(value ? -1 : 0);
            }

            _loopMidi = value;
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? TrackingEntity { get; set; } = null;

    [ViewVariables(VVAccess.ReadWrite)]
    public MapCoordinates? TrackingCoordinates { get; set; } = null;

    [ViewVariables]
    public BitArray FilteredChannels { get; } = new(RobustMidiEvent.MaxChannels);

    [ViewVariables(VVAccess.ReadWrite)]
    public byte? VelocityOverride { get; set; } = null;

    [ViewVariables(VVAccess.ReadWrite)]
    public IMidiRenderer? Master
    {
        get => _master;
        set
        {
            if (value == _master)
                return;

            if (CheckMasterCycle(value))
                throw new InvalidOperationException("Tried to set master to a child of this renderer!");

            if (_master is { Disposed: false })
            {
                try
                {
                    _master.OnMidiEvent -= SendMidiEvent;
                }
                catch
                {
                    // ignored
                }
            }

            _master = value;

            if (_master == null)
                return;

            _master.OnMidiEvent += SendMidiEvent;
            ApplyState(_master.RendererState, true);
            MidiBank = _midiBank;
        }
    }

    [ViewVariables, UsedImplicitly]
    private double CpuLoad => !_synth.Disposed ? _synth.CpuLoad : 0;

    public event Action<RobustMidiEvent>? OnMidiEvent;
    public event Action? OnMidiPlayerFinished;

    internal MidiRenderer(Settings settings, SoundFontLoader soundFontLoader, bool mono,
        IMidiManager midiManager, IAudioInternal clydeAudio, ITaskManager taskManager, ISawmill midiSawmill)
    {
        _midiManager = midiManager;
        _taskManager = taskManager;
        _midiSawmill = midiSawmill;

        Source = clydeAudio.CreateBufferedAudioSource(Buffers, true) ?? DummyBufferedAudioSource.Instance;
        Source.SampleRate = SampleRate;
        _settings = settings;
        _soundFontLoader = soundFontLoader;
        _synth = new Synth(_settings);
        _sequencer = new Sequencer(false);
        _debugRegister = _sequencer.RegisterClient("honk", DumpSequencerEvent);
        _robustRegister = _sequencer.RegisterClient("henk", SendAsRobustMidiEvent);

        // We need to register at least one synthesizer or the sequencer will refuse to work properly.
        _synthRegister = _sequencer.RegisterFluidsynth(_synth);

        _synth.AddSoundFontLoader(soundFontLoader);

        Mono = mono;
        Source.EmptyBuffers();
        Source.StartPlaying();
    }

    private void DumpSequencerEvent(uint time, SequencerEvent midiEvent)
    {
        // ReSharper disable once UseStringInterpolation
        _midiSawmill.Debug($"{time:D8}: {MidiManager.SequencerEventToString(midiEvent)}");

        midiEvent.Dest = _robustRegister;
        _sequencer.SendNow(midiEvent);
    }

    private void SendAsRobustMidiEvent(uint time, SequencerEvent midiEvent)
    {
        var robustEvent = _midiManager.FromSequencerEvent(midiEvent, time);

        // Check if the command is correct.
        if (robustEvent.Command != 0)
        {
            SendMidiEvent(robustEvent);
            midiEvent.Dispose();
        }
        else
        {
            // Unsupported command, send it to the synth directly.
            midiEvent.Dest = _synthRegister;
            _sequencer.SendNow(midiEvent);
        }
    }

    public bool OpenInput()
    {
        if (Disposed)
            return false;

        if (Status != MidiRendererStatus.File) CloseMidi();

        lock (_playerStateLock)
        {
            Status = MidiRendererStatus.Input;
            StopAllNotes();

            _driver = new MidiDriver(_settings, MidiDriverEventHandler);
        }

        return true;
    }

    public bool OpenMidi(ReadOnlySpan<byte> buffer)
    {
        if (Disposed)
            return false;

        if (Status == MidiRendererStatus.Input) CloseInput();

        lock (_playerStateLock)
        {
            Status = MidiRendererStatus.File;
            StopAllNotes();

            if (buffer.Length > MidiSizeLimit)
            {
                _midiSawmill.Error("MIDI file selected is too big! It was {0} MB but it should be less than {1} MB.",
                    buffer.Length * BytesToMegabytes, MidiSizeLimit * BytesToMegabytes);
                CloseMidi();
                return false;
            }

            _playerTotalTicks = 0;
            _player?.Dispose();
            _player = new NFluidsynth.Player(_synth);
            _player.SetPlaybackCallback(MidiPlayerEventHandler);
            _player.AddMem(buffer);
            _player.Seek(0);
            _player.Play();
            _player.SetLoop(LoopMidi ? -1 : 1);
        }

        return true;
    }

    public bool CloseInput()
    {
        if (Status != MidiRendererStatus.Input) return false;

        lock (_playerStateLock)
        {
            Status = MidiRendererStatus.None;
            _driver?.Dispose();
            _driver = null;
        }

        StopAllNotes();
        return true;
    }

    public bool CloseMidi()
    {
        if (Status != MidiRendererStatus.File) return false;
        lock (_playerStateLock)
        {
            Status = MidiRendererStatus.None;
            if (_player == null) return false;
            _player?.Stop();
            _player?.Join();
            _player?.Dispose();
            _player = null;
            _playerTotalTicks = 0;
        }

        StopAllNotes();
        return true;
    }

    private int MidiPlayerEventHandler(MidiEvent midiEvent)
    {
        if (Disposed || Status != MidiRendererStatus.File && _player?.Status == FluidPlayerStatus.Playing)
            return 0;

        var midiEv = _midiManager.FromFluidEvent(midiEvent, SequencerTick);
        midiEvent.Dispose();
        SendMidiEvent(midiEv);
        return 0;
    }

    private int MidiDriverEventHandler(MidiEvent midiEvent)
    {
        if (Disposed || Status != MidiRendererStatus.Input)
            return 0;

        var midiEv = _midiManager.FromFluidEvent(midiEvent, SequencerTick);
        midiEvent.Dispose();
        SendMidiEvent(midiEv);
        return 0;
    }

    public void StopAllNotes()
    {
        for (byte i = 0; i < ChannelCount; i++)
        {
            SendMidiEvent(RobustMidiEvent.AllNotesOff(i, SequencerTick));
        }
    }

    public void SystemReset()
    {
        SendMidiEvent(RobustMidiEvent.SystemReset(SequencerTick));
    }

    public void ClearAllEvents()
    {
        _sequencer.RemoveEvents(SequencerClientId.Wildcard, SequencerClientId.Wildcard, -1);
    }

    void IMidiRenderer.Render()
    {
        Render();
    }

    private void Render(int length = SampleRate / 250)
    {
        if (Disposed) return;

        var buffersProcessed = Source.GetNumberOfBuffersProcessed();

        if(buffersProcessed == Buffers)
            _midiSawmill.Warning("MIDI buffer overflow!");

        if (buffersProcessed == 0)
            return;

        var bufferLength = length * 2;

        unsafe
        {
            Span<int> buffers = stackalloc int[buffersProcessed];
            Span<float> audio = stackalloc float[bufferLength * buffers.Length];

            Source.GetBuffersProcessed(buffers);

            lock (_playerStateLock)
            {
                // _sequencer.Process(10);
                _synth?.WriteSampleFloat(length * buffers.Length, audio, 0, Mono ? 1 : 2,
                    audio, Mono ? length * buffers.Length : 1, Mono ? 1 : 2);
            }
            if (Mono) // Turn audio to mono
            {
                var l = length * buffers.Length;

                NumericsHelpers.Add(audio[..l], audio[l..]);
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
            // Fluidsynth's player sometimes doesn't set itself to done, so also check current tick vs. total ticks.
            if (Status == MidiRendererStatus.File && (_player?.Status == FluidPlayerStatus.Done || PlayerTick >= PlayerTotalTick))
            {
                _taskManager.RunOnMainThread(() => OnMidiPlayerFinished?.Invoke());
                CloseMidi();
            }
        }

        Source.StartPlaying();
    }

    public void ApplyState(MidiRendererState state, bool filterChannels = false)
    {
        lock (_playerStateLock)
        {
            _synth.SystemReset();

            for (var channel = 0; channel < ChannelCount; channel++)
            {
                if (filterChannels && !FilteredChannels[channel])
                    continue;

                _synth.AllNotesOff(channel);

                _synth.PitchBend(channel, state.PitchBend.AsSpan[channel]);
                _synth.ChannelPressure(channel, state.ChannelPressure.AsSpan[channel]);

                for (var controller = 0; controller < state.Controllers.AsSpan[channel].AsSpan.Length; controller++)
                {
                    var value = state.Controllers.AsSpan[channel].AsSpan[controller];

                    if (value == _synth.GetCC(channel, controller))
                        continue;

                    try
                    {
                        _synth.CC(channel, controller, value);
                    }
                    catch (FluidSynthInteropException e)
                    {
                        _midiSawmill.Error($"CH:{channel} CC:{controller} VAL:{value} {e.ToStringBetter()}");
                    }
                }

                var program = DisableProgramChangeEvent ? MidiProgram : state.Program.AsSpan[channel];
                _synth.ProgramChange(channel, program);

                for (var key = 0; key < state.NoteVelocities.AsSpan[channel].AsSpan.Length; key++)
                {
                    var velocity = state.NoteVelocities.AsSpan[channel].AsSpan[key];

                    if (velocity <= 0)
                        continue;

                    try
                    {
                        _synth.NoteOn(channel, key, velocity);
                    }
                    catch (FluidSynthInteropException e)
                    {
                        _midiSawmill.Error($"CH:{channel} KEY:{key} VEL:{velocity} {e.ToStringBetter()}");
                    }
                }
            }

            // Sorry PJB, I have to copy it.
            _rendererState = state;
        }
    }

    private void SendMidiEvent(RobustMidiEvent midiEvent)
    {
        SendMidiEvent(midiEvent, true);
    }

    public void SendMidiEvent(RobustMidiEvent midiEvent, bool raiseEvent)
    {
        if (Disposed)
            return;

        try
        {
            lock(_playerStateLock)
            {
                // Use MidiCommand as it's more readable with switch statements.
                switch (midiEvent.MidiCommand)
                {
                    case RobustMidiCommand.NoteOff:
                        _rendererState.NoteVelocities.AsSpan[midiEvent.Channel].AsSpan[midiEvent.Key] = 0;
                        _synth.NoteOff(midiEvent.Channel, midiEvent.Key);

                        break;
                    case RobustMidiCommand.NoteOn:
                        // Velocity 0 *can* represent a NoteOff event.
                        var velocity = midiEvent.Velocity;
                        if (velocity == 0)
                        {
                            _rendererState.NoteVelocities.AsSpan[midiEvent.Channel].AsSpan[midiEvent.Key] = 0;
                            _synth.NoteOn(midiEvent.Channel, midiEvent.Key, velocity);

                            break;
                        }

                        if (FilteredChannels[midiEvent.Channel])
                            break;

                        velocity = VelocityOverride ?? midiEvent.Velocity;

                        _rendererState.NoteVelocities.AsSpan[midiEvent.Channel].AsSpan[midiEvent.Key] = velocity;
                        _synth.NoteOn(midiEvent.Channel, midiEvent.Key, velocity);

                        break;
                    case RobustMidiCommand.AfterTouch:
                        _rendererState.NoteVelocities.AsSpan[midiEvent.Channel].AsSpan[midiEvent.Key] = midiEvent.Value;
                        _synth.KeyPressure(midiEvent.Channel, midiEvent.Key, midiEvent.Value);
                        break;

                    case RobustMidiCommand.ControlChange:
                        // CC0 is bank selection
                        if (midiEvent.Control == 0x0 && DisableProgramChangeEvent)
                            break;

                        _rendererState.Controllers.AsSpan[midiEvent.Channel].AsSpan[midiEvent.Control] = midiEvent.Value;
                        if(midiEvent.Control != 0x0)
                            _synth.CC(midiEvent.Channel, midiEvent.Control, midiEvent.Value);
                        else // Fluidsynth doesn't seem to respect CC0 as bank selection, so we have to do it manually.
                            _synth.BankSelect(midiEvent.Channel, midiEvent.Value);
                        break;

                    case RobustMidiCommand.ProgramChange:
                        if (DisableProgramChangeEvent)
                            break;

                        _rendererState.Program.AsSpan[midiEvent.Channel] = midiEvent.Program;
                        _synth.ProgramChange(midiEvent.Channel, midiEvent.Program);
                        break;

                    case RobustMidiCommand.ChannelPressure:
                        _rendererState.ChannelPressure.AsSpan[midiEvent.Channel] = midiEvent.Pressure;
                        _synth.ChannelPressure(midiEvent.Channel, midiEvent.Pressure);
                        break;

                    case RobustMidiCommand.PitchBend:
                        _rendererState.PitchBend.AsSpan[midiEvent.Channel] = (ushort)midiEvent.Pitch;
                        _synth.PitchBend(midiEvent.Channel, midiEvent.Pitch);
                        break;

                    // Sometimes MIDI files spam these for no good reason and I can't find any info on what they are.
                    case (RobustMidiCommand) 0x00:
                    case (RobustMidiCommand) 0x01:
                    case (RobustMidiCommand) 0x05:
                    case (RobustMidiCommand) 0x50: // MetaEvent -- SetTempo, handled by the player.
                        return;

                    case RobustMidiCommand.SystemMessage:
                        switch (midiEvent.Control)
                        {
                            case 0x0 when midiEvent.Status == 0xFF:
                                _rendererState = new MidiRendererState();
                                _synth.SystemReset();

                                // Reset the instrument to the one we were using.
                                if (DisableProgramChangeEvent)
                                {
                                    MidiBank = _midiBank;
                                    MidiProgram = _midiProgram;
                                }

                                break;

                            case 0x0B:
                                _rendererState.NoteVelocities = default;
                                _synth.AllNotesOff(midiEvent.Channel);
                                break;
                        }

                        break;

                    default:
                        _midiSawmill.Warning($"Unhandled midi event of type 0x{midiEvent.Command:X}! Event: {midiEvent}");
                        return;
                }
            }
        }
        catch (IndexOutOfRangeException)
        {
            // FIXME: Handle malicious MIDI events properly, do sanity so they can't cause this exception on the state.
        }
        catch (FluidSynthInteropException)
        {
            // This spams NoteOff errors most of the time for no good reason.
            //_midiSawmill.Error("Exception while sending midi event of type {0}: {1}", midiEvent.Type, e, midiEvent);
        }

        if (raiseEvent)
        {
            _taskManager.RunOnMainThread(() => OnMidiEvent?.Invoke(midiEvent));
        }
    }

    public void ScheduleMidiEvent(RobustMidiEvent midiEvent, uint time, bool absolute = false)
    {
        if (Disposed) return;

        var seqEv = _midiManager.ToSequencerEvent(midiEvent);
        seqEv.Dest = _debugEvents ? _debugRegister : _robustRegister;

        // If this is an old event, send it right now.
        if(absolute && time <= SequencerTick || !absolute && time <= 0)
            _sequencer.SendNow(seqEv);
        else
            _sequencer.SendAt(seqEv, time, absolute);
        seqEv.Dispose();
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
    }

    /// <inheritdoc />
    void IMidiRenderer.InternalDispose()
    {
        OnMidiEvent = null;
        OnMidiPlayerFinished = null;

        Source?.Dispose();
        _driver?.Dispose();

        // Do NOT dispose of the sequencer after the synth or it'll cause a segfault for some fucking reason.
        _sequencer?.UnregisterClient(_debugRegister);
        _sequencer?.UnregisterClient(_robustRegister);
        _sequencer?.UnregisterClient(_synthRegister);
        _sequencer?.Dispose();

        _synth?.Dispose();
        _player?.Dispose();
    }

    /// <summary>
    /// Check that a given renderer is not already a child of this renderer, i.e. it would introduce a cycle if set as master of this renderer.
    /// </summary>
    private bool CheckMasterCycle(IMidiRenderer? otherRenderer)
    {
        // Doesn't inside drift, cringe.

        while (otherRenderer != null)
        {
            if (otherRenderer == this)
                return true;

            otherRenderer = otherRenderer.Master;
        }

        return false;
    }
}
