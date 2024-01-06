using System.Collections.Generic;

namespace Robust.Client.SpeechSynthesis;

public interface ITtsManager
{
    bool Available { get; }
    IReadOnlyList<ITtsVoice> Voices { get; }
    void Speak(string text);
}

internal interface ITtsManagerInternal : ITtsManager
{
    void Initialize();
}
