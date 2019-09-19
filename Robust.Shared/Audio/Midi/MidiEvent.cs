using System;
using Commons.Music.Midi;
using Robust.Shared.Serialization;

namespace Robust.Shared.Audio.Midi
{
    [Serializable, NetSerializable]
    public class MidiEvent
    {
        public const byte NoteOffEvent = 128;
        public const byte NoteOnEvent = 144;
        public const byte CCEvent = 176;
        public const byte PitchEvent = 224;

        public static MidiEvent NoteOn(byte key, byte velocity, byte channel = 0)
        {
            return new MidiEvent(NoteOnEvent, new []{key, velocity}, channel);
        }

        public static MidiEvent NoteOff(byte key, byte channel = 0)
        {
            return new MidiEvent(NoteOffEvent, new []{key}, channel);
        }

        public static MidiEvent CC(byte channel = 0)
        {
            return new MidiEvent(CCEvent, new []{channel}, channel);
        }

        public static MidiEvent Pitch(byte val1, byte val2, byte channel = 0)
        {
            return new MidiEvent(PitchEvent, new []{val1, val2}, channel);
        }

        public MidiEvent(byte eventType, byte[] extraData = null, byte channel = 0)
        {
            EventType = eventType;
            ExtraData = extraData;
            Channel = channel;
        }

        public byte[] Data
        {
            get
            {
                var data = new byte[1+ExtraData.Length];
                data[0] = EventType;
                for (var i = 0; i < ExtraData.Length; i++)
                {
                    var d = ExtraData[i];
                    data[i+1] = d;
                }
                return null;
            }
        }

        public readonly byte EventType;
        public readonly byte Channel;
        public readonly byte[] ExtraData;

        public static explicit operator MidiEvent(Commons.Music.Midi.MidiEvent midiEvent)
        {
            return new MidiEvent(midiEvent.EventType, midiEvent.ExtraData, midiEvent.Channel);
        }

        public static explicit operator MidiEvent(MidiReceivedEventArgs midiEvent)
        {
            var b = new byte[]{midiEvent.Data[1], midiEvent.Data[2]};
            return new MidiEvent(midiEvent.Data[0], b);
        }
    }
}
