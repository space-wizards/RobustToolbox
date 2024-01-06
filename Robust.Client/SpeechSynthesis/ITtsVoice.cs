namespace Robust.Client.SpeechSynthesis;

/// <summary>
/// Represents an available Text-To-Speech voice for speech synthesis.
/// </summary>
/// <seealso cref="ITtsManager"/>
public interface ITtsVoice
{
    string DisplayName { get; }
    string Description { get; }
    string Id { get; }
    string Language { get; }
    TtsVoiceGender Gender { get; }
}

public enum TtsVoiceGender : byte
{
    Other,
    Male,
    Female
}
