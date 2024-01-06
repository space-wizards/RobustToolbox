#if WINDOWS

using System;
using System.Collections.Generic;
using System.IO;
using Windows.Media.SpeechSynthesis;
using Robust.Client.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Client.SpeechSynthesis;

/// <summary>
/// Implementation of <see cref="ITtsManager"/> that uses <c>Windows.Media.SpeechSynthesis</c>.
/// </summary>
internal sealed class TtsManagerWinRT : ITtsManagerInternal
{
    [Dependency] private readonly IAudioManager _audioManager = default!;

    private readonly List<TtsVoice> _voices = [];

    public bool Available => true;
    public IReadOnlyList<ITtsVoice> Voices => _voices;

    public void Initialize()
    {
        foreach (var voice in SpeechSynthesizer.AllVoices)
        {
            _voices.Add(new TtsVoice(
                voice.Id,
                voice.DisplayName,
                voice.Description,
                voice.Language,
                FromWinRT(voice.Gender)));
        }
    }

    public async void Speak(string text)
    {
        using var synth = new SpeechSynthesizer();
        var sw = RStopwatch.StartNew();
        using var result = await synth.SynthesizeTextToStreamAsync(text);

        await using var stream = result.AsStreamForRead();

        var sample = _audioManager.LoadAudioWav(stream);

        var source = _audioManager.CreateAudioSource(sample)!;

        source.Global = true;
        source.StartPlaying();
    }

    private static TtsVoiceGender FromWinRT(VoiceGender gender)
    {
        return gender switch
        {
            VoiceGender.Male => TtsVoiceGender.Male,
            VoiceGender.Female => TtsVoiceGender.Female,
            _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null)
        };
    }
}

#endif
