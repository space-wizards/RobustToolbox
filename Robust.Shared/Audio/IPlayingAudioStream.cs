namespace Robust.Shared.Audio;

/// <summary>
/// Basic audio stream.
/// </summary>
public interface IPlayingAudioStream
{
    bool IsPlaying { get; }

    bool Done { get; internal set; }

    void Dispose();
}
