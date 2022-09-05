using System;
using System.Runtime.InteropServices;
using Robust.Shared.Utility;

namespace Robust.Client.Audio.Midi;

public struct MidiRendererState
{
    internal FixedArray16<FixedArray128<byte>> NoteVelocities;
    internal FixedArray16<FixedArray128<byte>> Controllers;
    internal FixedArray16<byte> Program;
    internal FixedArray16<byte> ChannelPressure;
    internal FixedArray16<ushort> PitchBend;

    internal Span<byte> AsSpan => MemoryMarshal.CreateSpan(ref NoteVelocities._00._00, 4160);

    static unsafe MidiRendererState()
    {
        var s = new MidiRendererState();
        DebugTools.Assert(s.AsSpan.Length == sizeof(MidiRendererState),
            $"{nameof(MidiRendererState)}'s {nameof(AsSpan)} length does not match struct size! Was: {s.AsSpan.Length} Expected: {sizeof(MidiRendererState)}");
    }

    public MidiRendererState()
    {
        NoteVelocities = default;
        Program = default;
        ChannelPressure = default;
        PitchBend = default;
        Controllers = default;

        // PitchBend is at 8192 by default.
        PitchBend.AsSpan.Fill(8192);

        // Controller defaults
        Controllers.AsSpan.Fill(new FixedArray128<byte>
        {
            // Bank selection default
            _00 = 0,

            // Volume controller default
            _07 = 100,

            // Balance controller default
            _08 = 64,

            // Pan controller default
            _10 = 64,

            // Expression controller default
            _11 = 127,

            // Controller 11 default
            _43 = 127,

            // Sound controllers 1 to 10 defaults
            _70 = 64,
            _71 = 64,
            _72 = 64,
            _73 = 64,
            _74 = 64,
            _75 = 64,
            _76 = 64,
            _77 = 64,
            _78 = 64,
            _79 = 64,

            // Portamento default
            _84 = 255,

            // Non-Registered Parameter Number defaults
            _98 = 127, // LSB
            _99 = 127, // MSB

            // Registered Parameter Number defaults
            _100 = 127, // LSB
            _101 = 127, // MSB
        });
    }
}
