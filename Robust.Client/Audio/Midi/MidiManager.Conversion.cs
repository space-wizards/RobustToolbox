using NFluidsynth;
using Robust.Shared.Audio.Midi;

namespace Robust.Client.Audio.Midi;

internal sealed partial class MidiManager
{
    public RobustMidiEvent FromFluidEvent(MidiEvent midiEvent, uint tick)
    {
        var status = RobustMidiEvent.MakeStatus((byte) midiEvent.Channel, (byte) midiEvent.Type);

        // Control is always the first data byte. Value is always the second data byte. Fluidsynth's API ain't great.
        var data1 = (byte) midiEvent.Control;
        var data2 = (byte) midiEvent.Value;

        // PitchBend is handled specially.
        if (midiEvent.Type == (int) RobustMidiCommand.PitchBend)
        {
            // We pack pitch into both data values.
            var pitch = (ushort) midiEvent.Pitch;
            data1 = (byte) pitch;
            data2 = (byte) (pitch >> 8);
        }

        return new RobustMidiEvent(status, data1, data2, tick);
    }

    public SequencerEvent ToSequencerEvent(RobustMidiEvent midiEvent)
    {
        var sequencerEvent = new SequencerEvent();

        switch (midiEvent.MidiCommand)
        {
            case RobustMidiCommand.NoteOff:
                sequencerEvent.NoteOff(midiEvent.Channel, midiEvent.Key);
                break;

            case RobustMidiCommand.NoteOn:
                sequencerEvent.NoteOn(midiEvent.Channel, midiEvent.Key, midiEvent.Velocity);
                break;

            case RobustMidiCommand.AfterTouch:
                sequencerEvent.KeyPressure(midiEvent.Channel, midiEvent.Key, midiEvent.Value);
                break;

            case RobustMidiCommand.ControlChange:
                sequencerEvent.ControlChange(midiEvent.Channel, midiEvent.Control, midiEvent.Value);
                break;

            case RobustMidiCommand.ProgramChange:
                sequencerEvent.ProgramChange(midiEvent.Channel, midiEvent.Program);
                break;

            case RobustMidiCommand.ChannelPressure:
                sequencerEvent.ChannelPressure(midiEvent.Channel, midiEvent.Value);
                break;

            case RobustMidiCommand.PitchBend:
                sequencerEvent.PitchBend(midiEvent.Channel, midiEvent.Pitch);
                break;

            case RobustMidiCommand.SystemMessage:
                switch (midiEvent.Control)
                {
                    case 0x0 when midiEvent.Status == 0xFF:
                        sequencerEvent.SystemReset();
                        break;

                    case 0x0B:
                        sequencerEvent.AllNotesOff(midiEvent.Channel);
                        break;

                    default:
                        _midiSawmill.Warning($"Tried to convert unsupported event to sequencer event:\n{midiEvent}");
                        break;
                }

                break;

            default:
                _midiSawmill.Warning($"Tried to convert unsupported event to sequencer event:\n{midiEvent}");
                break;
        }

        return sequencerEvent;
    }

    public RobustMidiEvent FromSequencerEvent(SequencerEvent midiEvent, uint tick)
    {
        byte channel = (byte) midiEvent.Channel;
        RobustMidiCommand command = 0x0;
        byte data1 = 0;
        byte data2 = 0;

        switch (midiEvent.Type)
        {
            case FluidSequencerEventType.NoteOn:
                command = RobustMidiCommand.NoteOn;
                data1 = (byte) midiEvent.Key;
                data2 = (byte) midiEvent.Velocity;
                break;

            case FluidSequencerEventType.NoteOff:
                command = RobustMidiCommand.NoteOff;
                data1 = (byte) midiEvent.Key;
                break;

            case FluidSequencerEventType.PitchBend:
                command = RobustMidiCommand.PitchBend;
                // We pack pitch into both data values
                var pitch = (ushort) midiEvent.Pitch;
                data1 = (byte) pitch;
                data2 = (byte) (pitch >> 8);
                break;

            case FluidSequencerEventType.ProgramChange:
                command = RobustMidiCommand.ProgramChange;
                data1 = (byte) midiEvent.Program;
                break;

            case FluidSequencerEventType.KeyPressure:
                command = RobustMidiCommand.AfterTouch;
                data1 = (byte) midiEvent.Key;
                data2 = (byte) midiEvent.Value;
                break;

            case FluidSequencerEventType.ControlChange:
                command = RobustMidiCommand.ControlChange;
                data1 = (byte) midiEvent.Control;
                data2 = (byte) midiEvent.Value;
                break;

            case FluidSequencerEventType.ChannelPressure:
                command = RobustMidiCommand.ChannelPressure;
                data1 = (byte) midiEvent.Value;
                break;

            case FluidSequencerEventType.AllNotesOff:
                command = RobustMidiCommand.SystemMessage;
                data1 = 0x0B;
                break;

            case FluidSequencerEventType.SystemReset:
                command = RobustMidiCommand.SystemMessage;
                channel = 0x0F;
                break;

            default:
                _midiSawmill.Warning($"Unsupported Sequencer Event: {tick:D8}: {SequencerEventToString(midiEvent)}");
                break;
        }

        return new RobustMidiEvent(RobustMidiEvent.MakeStatus(channel, (byte)command), data1, data2, tick);
    }
}
