using System.Collections.Generic;

namespace Robust.Client.SpeechSynthesis;

internal sealed class TtsManagerDummy : ITtsManagerInternal
{
    public bool Available => false;

    public IReadOnlyList<ITtsVoice> Voices { get; } = new[]
    {
        new TtsVoice(
            "Dummy",
            "Dummy headless voice",
            "Robust.Client.SpeechSynthesis.TtsManagerHeadless.Dummy",
            "en-US",
            TtsVoiceGender.Male)
    };

    public void Speak(string text)
    {
        // Nada.
    }

    public void Initialize()
    {

    }
}
