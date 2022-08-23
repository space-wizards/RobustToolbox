namespace Robust.Shared.Audio.Midi;

/// <summary>
///     Helper enum that keeps track of all MIDI commands that Robust currently supports.
/// </summary>
public enum RobustMidiCommand : byte
{
    /// <summary>
    ///     NoteOff event.              <br/>
    ///     data1 - Key,                <br/>
    ///     data2 - undefined
    /// </summary>
    NoteOff          = 0x80,

    /// <summary>
    ///     NoteOn event.               <br/>
    ///     data1 - Key,                <br/>
    ///     data2 - Velocity
    /// </summary>
    NoteOn           = 0x90,

    /// <summary>
    ///     AfterTouch event.           <br/>
    ///     data1 - Key,                <br/>
    ///     data2 - Value
    /// </summary>
    /// <remarks>Also known as "KeyPressure".</remarks>
    AfterTouch       = 0xA0,

    /// <summary>
    ///     ControlChange (CC) event.   <br/>
    ///     data1 - Control,            <br/>
    ///     data2 - Value
    /// </summary>
    ControlChange    = 0xB0,

    /// <summary>
    ///     ProgramChange event.        <br/>
    ///     data1 - Program,            <br/>
    ///     data2 - undefined
    /// </summary>
    ProgramChange    = 0xC0,

    /// <summary>
    ///     ChannelPressure event.      <br/>
    ///     data1 - Value,              <br/>
    ///     data2 - undefined
    /// </summary>
    ChannelPressure  = 0xD0,

    /// <summary>
    ///     PitchBend event.            <br/>
    ///     data1 - Lower Pitch Nibble, <br/>
    ///     data2 - Higher Pitch Nibble
    /// </summary>
    PitchBend        = 0xE0,

    /// <summary>
    ///     SystemMessage event.        <br/>
    ///     data1 - Control             <br/>
    ///     data2 - undefined
    /// </summary>
    SystemMessage    = 0xF0,
}
