using System.IO;
using Robust.Client.Audio;

namespace Robust.Client.Graphics
{
    public interface IClydeAudio
    {
        // AUDIO SYSTEM DOWN BELOW.
        AudioStream LoadAudioOggVorbis(Stream stream, string? name = null);
        AudioStream LoadAudioWav(Stream stream, string? name = null);
        AudioStream LoadAudioRaw(short[] samples, int channels, int sampleRate);

        void SetMasterVolume(float newVolume);

        IClydeAudioSource CreateAudioSource(AudioStream stream);
        IClydeBufferedAudioSource CreateBufferedAudioSource(int buffers, bool floatAudio=false);
    }
}
