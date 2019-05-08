using Robust.Client.Audio;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.Utility;
using System.IO;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.IoC;

namespace Robust.Client.ResourceManagement
{
    public class AudioResource : BaseResource
    {
        public AudioStream AudioStream { get; private set; }

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            if (!cache.ContentFileExists(path))
            {
                throw new FileNotFoundException("Content file does not exist for audio sample.");
            }

            using (var fileStream = cache.ContentFileRead(path))
            {
                AudioStream = IoCManager.Resolve<IClyde>().LoadAudioOggVorbis(fileStream);
            }
        }

        public static implicit operator AudioStream(AudioResource res)
        {
            return res.AudioStream;
        }
    }
}
