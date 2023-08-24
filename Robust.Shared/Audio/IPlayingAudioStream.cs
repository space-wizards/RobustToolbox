namespace Robust.Shared.Audio;
public interface IPlayingAudioStream
{
    public uint Identifier { get; }
    
    void Stop();
}
