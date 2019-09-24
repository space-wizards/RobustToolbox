using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Audio.Midi
{
    [Serializable, NetSerializable]
    public class MidiEvent
    {
        public int Type { get; set; }

        public int Channel { get; set; }

        public int Key { get; set; }

        public int Velocity { get; set; }

        public int Control { get; set; }

        public int Value { get; set; }

        public int Program { get; set; }

        public int Pitch { get; set; }

        public static explicit operator MidiEvent(NFluidsynth.MidiEvent midiEvent)
        {
            return new MidiEvent()
            {
                Type = midiEvent.Type,
                Channel = midiEvent.Channel,
                Control = midiEvent.Control,
                Key = midiEvent.Key,
                Pitch = midiEvent.Pitch,
                Program = midiEvent.Program,
                Value = midiEvent.Value,
                Velocity = midiEvent.Velocity,
            };
        }

        public static implicit operator NFluidsynth.MidiEvent(MidiEvent midiEvent)
        {
            return new NFluidsynth.MidiEvent()
            {
                Type = midiEvent.Type,
                Channel = midiEvent.Channel,
                Control = midiEvent.Control,
                Key = midiEvent.Key,
                Pitch = midiEvent.Pitch,
                Program = midiEvent.Program,
                Value = midiEvent.Value,
                Velocity = midiEvent.Velocity,
            };
        }
    }
}
