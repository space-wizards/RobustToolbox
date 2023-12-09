namespace Robust.Client.Audio;

/// <summary>
/// Public audio API for stuff that can't go through <see cref="AudioSystem"/>
/// </summary>
public interface IAudioManager
{
    void SetMasterGain(float gain);
}
