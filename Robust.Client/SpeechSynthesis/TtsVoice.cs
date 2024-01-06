namespace Robust.Client.SpeechSynthesis;

internal sealed record TtsVoice(
    string Id,
    string DisplayName,
    string Description,
    string Language,
    TtsVoiceGender Gender)
    : ITtsVoice;
