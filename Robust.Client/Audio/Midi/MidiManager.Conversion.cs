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
                sequencerEvent.PitchBend(midiEvent.Channel, midiEvent.Value);
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
}
