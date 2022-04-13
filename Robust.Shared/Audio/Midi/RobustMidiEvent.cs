using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Audio.Midi
{
    /// <summary>
    ///     This class is a lightweight data representation of a Midi Event.
    /// </summary>
    [Serializable, NetSerializable]
    public readonly struct RobustMidiEvent
    {
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
        public int Pitch => (Data2 << 8) | Data1;
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

        public override string ToString()
        {
            return $"{base.ToString()} >> CHANNEL: 0x{Channel:X} || COMMAND: 0x{Command:X} {MidiCommand} || DATA1: 0x{Data1:X} || DATA2: 0x{Data2:X} || TICK: {Tick} <<";
        }

        #region Static Methods

        /// <summary>
        ///     Returns a status byte given a channel byte and a command type byte.
        /// </summary>
        public static byte MakeStatus(byte channel, byte command)
        {
            return (byte) (command | channel);
        }

        /// <summary>
        ///     Creates and returns an event to turn all notes off on a given channel.
        /// </summary>
        public static RobustMidiEvent AllNotesOff(byte channel, uint tick)
        {
            return new RobustMidiEvent(MakeStatus(channel, (byte)RobustMidiCommand.SystemMessage), 0x0B, 0x0, tick);
        }

        /// <summary>
        ///     Creates and returns an event to reset all controllers.
        /// </summary>
        public static RobustMidiEvent ResetAllControllers(uint tick)
        {
            return new RobustMidiEvent((byte)RobustMidiCommand.ControlChange, 0x79, 0x0, tick);
        }

        /// <summary>
        ///     Creates and returns a system reset event.
        /// </summary>
        public static RobustMidiEvent SystemReset(uint tick)
        {
            return new RobustMidiEvent((byte) RobustMidiCommand.SystemMessage | 0x0F, 0x0, 0x0, tick);
        }

        #endregion
    }
}

