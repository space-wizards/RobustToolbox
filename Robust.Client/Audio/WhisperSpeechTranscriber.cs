using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;

namespace Robust.Client.Audio;

/// <summary>
/// <see cref="ISpeechTranscriber"/> backed by Whisper.net (a managed wrapper around whisper.cpp), running a
/// local ggml model. See <see cref="ISpeechTranscriber"/> for why this lives in the engine.
/// </summary>
internal sealed class WhisperSpeechTranscriber : ISpeechTranscriber
{
    private const int SampleRate = 16_000;

    private readonly WhisperFactory _factory;
    private readonly WhisperProcessor _processor;

    public WhisperSpeechTranscriber(string modelPath, string language)
    {
        _factory = WhisperFactory.FromPath(modelPath);
        _processor = _factory.CreateBuilder()
            .WithLanguage(language)
            .Build();
    }

    public async Task<string> TranscribeAsync(ReadOnlyMemory<short> pcm, CancellationToken cancel = default)
    {
        // Whisper expects 16 kHz mono float samples normalised to [-1, 1].
        var span = pcm.Span;
        var samples = new float[span.Length];
        for (var i = 0; i < span.Length; i++)
            samples[i] = span[i] / 32768f;

        var builder = new StringBuilder();
        await foreach (var segment in _processor.ProcessAsync(samples, cancel))
        {
            builder.Append(segment.Text);
        }

        return builder.ToString().Trim();
    }

    public void Dispose()
    {
        _processor.Dispose();
        _factory.Dispose();
    }
}
