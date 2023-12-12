using Robust.Client.Audio.Sources;
using Robust.Shared.Audio.Sources;

namespace Robust.Client.Audio;

/// <summary>
/// Public audio API for stuff that can't go through <see cref="AudioSystem"/>
/// </summary>
public interface IAudioManager
{
    IAudioSource? CreateAudioSource(AudioStream stream);

    void SetMasterGain(float gain);
}
