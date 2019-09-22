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
    }
}
