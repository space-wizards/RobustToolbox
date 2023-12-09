using System.Collections.Generic;
using NFluidsynth;
using Robust.Shared.Audio.Midi;

namespace Robust.Client.Audio.Midi;

public interface IMidiManager
{
    /// <summary>
    ///     A read-only list of all existing MIDI Renderers.
    /// </summary>
    IReadOnlyList<IMidiRenderer> Renderers { get; }

    /// <summary>
    ///     If true, MIDI support is available.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    ///     Gain of audio.
    /// </summary>
    float Gain { get; set; }

    /// <summary>
    ///     This method tries to return a midi renderer ready to be used.
    ///     You only need to set the <see cref="IMidiRenderer.MidiProgram"/> afterwards.
    /// </summary>
    /// <remarks>
    ///     This method can fail if MIDI support is not available.
    /// </remarks>
    /// <returns>
    ///     <c>null</c> if MIDI support is not available.
    /// </returns>
    IMidiRenderer? GetNewRenderer(bool mono = true);

    /// <summary>
    ///     Creates a <see cref="RobustMidiEvent"/> given a <see cref="MidiEvent"/> and a sequencer tick.
    /// </summary>
    RobustMidiEvent FromFluidEvent(MidiEvent midiEvent, uint tick);

    /// <summary>
    ///     Creates a <see cref="SequencerEvent"/> given a <see cref="RobustMidiEvent"/>.
    ///     Be sure to dispose of the result after you've used it.
    /// </summary>
    SequencerEvent ToSequencerEvent(RobustMidiEvent midiEvent);

    /// <summary>
    ///     Creates a <see cref="RobustMidiEvent"/> given a <see cref="SequencerEvent"/> and a sequencer tick.
    /// </summary>
    RobustMidiEvent FromSequencerEvent(SequencerEvent midiEvent, uint tick);

    /// <summary>
    ///     Method called every frame.
    ///     Should be used to update positional audio.
    /// </summary>
    /// <param name="frameTime"></param>
    void FrameUpdate(float frameTime);

    void Shutdown();
}
