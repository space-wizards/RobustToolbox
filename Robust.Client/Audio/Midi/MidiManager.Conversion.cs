using System;
using NFluidsynth;
using Robust.Shared.Audio.Midi;

namespace Robust.Client.Audio.Midi;

internal sealed partial class MidiManager
{
    public RobustMidiEvent FromFluidEvent(MidiEvent midiEvent, uint tick)
    {
        var status = RobustMidiEvent.MakeStatus((byte) midiEvent.Channel, (byte) midiEvent.Type);

        byte data1 = 0;
        byte data2 = 0;

        switch ((RobustMidiCommand) midiEvent.Type)
        {
            case RobustMidiCommand.NoteOff:
                data1 = (byte) midiEvent.Key;
                break;

            case RobustMidiCommand.NoteOn:
                data1 = (byte) midiEvent.Key;
                data2 = (byte) midiEvent.Velocity;
                break;

            case RobustMidiCommand.AfterTouch:
                data1 = (byte) midiEvent.Key;
                data2 = (byte) midiEvent.Value;
                break;

            case RobustMidiCommand.ControlChange:
                data1 = (byte) midiEvent.Control;
                data2 = (byte) midiEvent.Value;
                break;

            case RobustMidiCommand.ProgramChange:
                data1 = (byte) midiEvent.Program;
                break;

            case RobustMidiCommand.ChannelPressure:
                data1 = (byte)midiEvent.Value;
                break;

            case RobustMidiCommand.PitchBend:
                // We pack pitch into both data values.
                var pitch = (ushort) midiEvent.Pitch;
                var data = BitConverter.GetBytes(pitch);
                data1 = data[0];
                data2 = data[1];
                break;

            case RobustMidiCommand.SystemMessage:
                data1 = (byte) midiEvent.Control;
                break;

            default:
                break;
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
                    case 0x0B:
                        sequencerEvent.AllNotesOff(midiEvent.Channel);
                        break;
                }
                break;

            default:
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
                var data = BitConverter.GetBytes(pitch);
                data1 = data[0];
                data2 = data[1];
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

            // Any other events will be sent to the synth directly by the sequencer.
            default:
                /*
                 _midiSawmill.Error(string.Format(
                    "Unsupported Sequencer Event: {0:D8}: {1} chan:{2:D2} key:{3:D5} bank:{4:D2} ctrl:{5:D5} dur:{6:D5} pitch:{7:D5} prog:{8:D3} val:{9:D5} vel:{10:D5}",
                    tick,
                    midiEvent.Type.ToString().PadLeft(22),
                    midiEvent.Channel,
                    midiEvent.Key,
                    midiEvent.Bank,
                    midiEvent.Control,
                    midiEvent.Duration,
                    midiEvent.Pitch,
                    midiEvent.Program,
                    midiEvent.Value,
                    midiEvent.Velocity));
                */
                break;
        }

        return new RobustMidiEvent(RobustMidiEvent.MakeStatus(channel, (byte)command), data1, data2, tick);
    }
}
