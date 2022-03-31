namespace Robust.Shared.Audio.Midi;

/// <summary>
///     Helper enum that keeps track of all MIDI commands that Robust currently supports.
/// </summary>
public enum RobustMidiCommand : byte
{
    NoteOff          = 0x80,
    NoteOn           = 0x90,
    AfterTouch       = 0xA0,
    ControlChange    = 0xB0,
    ProgramChange    = 0xC0,
    ChannelPressure  = 0xD0,
    PitchBend        = 0xE0,
    SystemMessage    = 0xF0,
}
