using System;
using Robust.Client.Audio;
using Robust.Shared.Utility;
using System.IO;
using Robust.Client.Graphics;
using Robust.Shared.IoC;

namespace Robust.Client.ResourceManagement
{
    public class AudioResource : BaseResource
    {
        public AudioStream AudioStream { get; private set; } = default!;

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            if (!cache.ContentFileExists(path))
            {
                throw new FileNotFoundException("Content file does not exist for audio sample.");
            }

            using (var fileStream = cache.ContentFileRead(path))
            {
                var clyde = IoCManager.Resolve<IClydeAudio>();
                if (path.Extension == "ogg")
                {
                    AudioStream = clyde.LoadAudioOggVorbis(fileStream, path.ToString());
                }
                else if (path.Extension == "wav")
                {
                    AudioStream = clyde.LoadAudioWav(fileStream, path.ToString());
                }
                else
                {
                    throw new NotSupportedException("Unable to load audio files outside of ogg Vorbis or PCM wav");
                }
            }
        }

        public static implicit operator AudioStream(AudioResource res)
        {
            return res.AudioStream;
        }
    }
}
