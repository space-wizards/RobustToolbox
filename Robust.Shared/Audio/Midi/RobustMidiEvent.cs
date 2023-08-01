using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Robust.Shared.Audio.Midi
{
    /// <summary>
    ///     This class is a lightweight data representation of a Midi Event.
    /// </summary>
    [Serializable, NetSerializable]
    public readonly struct RobustMidiEvent
    {
        public const int MaxChannels = 16;
        public const int PercussionChannel = 9;

        #region Data

        /// <summary>
        ///     Byte that stores both Command Type and Channel.
        /// </summary>
        public readonly byte Status;

        public readonly byte Data1;
        public readonly byte Data2;

        /// <summary>
        ///     Sequencer tick to schedule this event at.
        /// </summary>
        public readonly uint Tick;

        #endregion

        #region Properties

        public int Channel => Status & 0x0F; // Low nibble.
        public int Command => Status & 0xF0; // High nibble.
        public RobustMidiCommand MidiCommand => (RobustMidiCommand) Command;
        public byte Key => Data1;
        public byte Velocity => Data2;
        public byte Control => Data1;
        public byte Value => Data2;
        public int Pitch => (Data2 << 8) | Data1; // Actually fits in a short
        public byte Pressure => Data1;
        public byte Program => Data1;

        #endregion

        public RobustMidiEvent(byte status, byte data1, byte data2, uint tick)
        {
            Status = status;
            Data1 = data1;
            Data2 = data2;
            Tick = tick;
        }

        /// <summary>
        ///     Clones another event but with a different tick value.
        /// </summary>
        public RobustMidiEvent(RobustMidiEvent ev, uint tick)
        {
            Status = ev.Status;
            Data1 = ev.Data1;
            Data2 = ev.Data2;
            Tick = tick;
        }

        public override string ToString()
        {
            return $"{base.ToString()} >> CHANNEL: 0x{Channel:X} || COMMAND: 0x{Command:X} {MidiCommand} || DATA1: 0x{Data1:X} || DATA2: 0x{Data2:X} || TICK: {Tick} <<";
        }

        #region Static Methods

        /// <summary> Returns a status byte given a channel byte and a command type byte. </summary>
        public static byte MakeStatus(byte channel, byte command)
        {
            return (byte) (command | channel);
        }

        /// <summary> Returns a status byte given a channel byte and a command type. </summary>
        public static byte MakeStatus(byte channel, RobustMidiCommand command)
        {
            return MakeStatus(channel, (byte) command);
        }

        /// <summary> Creates and returns an event to turn off a note on a given channel. </summary>
        public static RobustMidiEvent NoteOff(byte channel, byte key, uint tick)
        {
            return new RobustMidiEvent(MakeStatus(channel, RobustMidiCommand.NoteOff), key, 0, tick);
        }

        /// <summary> Creates and returns an event to turn on a note on a given channel. </summary>
        public static RobustMidiEvent NoteOn(byte channel, byte key, byte velocity, uint tick)
        {
            return new RobustMidiEvent(MakeStatus(channel, RobustMidiCommand.NoteOn), key, velocity, tick);
        }

        /// <summary> Creates and returns an event to change the velocity on an active note on a given channel. </summary>
        public static RobustMidiEvent AfterTouch(byte channel, byte key, byte value, uint tick)
        {
            return new RobustMidiEvent(MakeStatus(channel, RobustMidiCommand.AfterTouch), key, value, tick);
        }

        /// <summary> Creates and returns an event to change a specific controller value on a given channel. </summary>
        public static RobustMidiEvent ControlChange(byte channel, byte control, byte value, uint tick)
        {
            return new RobustMidiEvent(MakeStatus(channel, RobustMidiCommand.ControlChange), control, value, tick);
        }

        /// <summary> Creates and returns an event to change the program on a given channel. </summary>
        public static RobustMidiEvent ProgramChange(byte channel, byte program, uint tick)
        {
            return new RobustMidiEvent(MakeStatus(channel, RobustMidiCommand.ProgramChange), program, 0x0, tick);
        }

        /// <summary> Creates and returns an event to change the note pressure on a given channel. </summary>
        public static RobustMidiEvent ChannelPressure(byte channel, byte pressure, uint tick)
        {
            return new RobustMidiEvent(MakeStatus(channel, RobustMidiCommand.ChannelPressure), pressure, 0x0, tick);
        }

        /// <summary> Creates and returns an event to bend the pitch on a given channel. </summary>
        public static RobustMidiEvent PitchBend(byte channel, ushort pitch, uint tick)
        {
            return new RobustMidiEvent(MakeStatus(channel, RobustMidiCommand.PitchBend), (byte) pitch, (byte) (pitch >> 8), tick);
        }

        /// <summary> Creates and returns an event to select the bank on a given channel. </summary>
        public static RobustMidiEvent BankSelect(byte channel, byte bank, uint tick)
        {
            return new RobustMidiEvent(MakeStatus(channel, RobustMidiCommand.ControlChange), 0x0, bank, tick);
        }

        /// <summary> Creates and returns an event to turn all notes off on a given channel. </summary>
        public static RobustMidiEvent AllNotesOff(byte channel, uint tick)
        {
            return new RobustMidiEvent(MakeStatus(channel, RobustMidiCommand.SystemMessage), 0x0B, 0x0, tick);
        }

        /// <summary> Creates and returns an event to reset all controllers. </summary>
        public static RobustMidiEvent ResetAllControllers(uint tick)
        {
            return new RobustMidiEvent((byte)RobustMidiCommand.ControlChange, 0x79, 0x0, tick);
        }

        /// <summary> Creates and returns a system message event. </summary>
        public static RobustMidiEvent SystemMessage(byte channel, byte control, uint tick)
        {
            return new RobustMidiEvent(MakeStatus(channel, RobustMidiCommand.SystemMessage), control, 0x0, tick);
        }

        /// <summary> Creates and returns a system reset event. </summary>
        public static RobustMidiEvent SystemReset(uint tick)
        {
            return new RobustMidiEvent((byte)RobustMidiCommand.SystemMessage | 0x0F, 0x0, 0x0, tick);
        }

        #endregion
    }
}

