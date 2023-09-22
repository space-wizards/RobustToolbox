using System;
using Robust.Shared.Utility;
using System.IO;
using Robust.Shared.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.ResourceManagement;

namespace Robust.Client.ResourceManagement;

public sealed class AudioResource : BaseResource
{
    public AudioStream AudioStream { get; private set; } = default!;

    public override void Load(IDependencyCollection dependencies, ResPath path)
    {
        var cache = dependencies.Resolve<IResourceManager>();

        if (!cache.ContentFileExists(path))
        {
            throw new FileNotFoundException("Content file does not exist for audio sample.");
        }

        using (var fileStream = cache.ContentFileRead(path))
        {
            if (path.Extension == "ogg")
            {
                AudioStream = dependencies.Resolve<SharedAudioManager>().LoadAudioOggVorbis(fileStream, path.ToString());
            }
            else if (path.Extension == "wav")
            {
                AudioStream = dependencies.Resolve<SharedAudioManager>().LoadAudioWav(fileStream, path.ToString());
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
