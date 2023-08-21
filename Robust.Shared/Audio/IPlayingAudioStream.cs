namespace Robust.Shared.Audio;
public interface IPlayingAudioStream
{
    void SetAudioParams(AudioParams parameters);

    AudioParams GetAudioParams();
    
    void Stop();
}
