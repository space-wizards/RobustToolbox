using System;
using System.IO;
using System.Threading;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement;

public sealed class AudioResource : BaseResource
{
    public AudioStream AudioStream { get; private set; } = default!;

    public void Load(AudioStream stream)
    {
        AudioStream = stream;
    }

    public override void Load(IDependencyCollection dependencies, ResPath path)
    {
        var cache = dependencies.Resolve<IResourceManager>();

        if (!cache.ContentFileExists(path))
        {
            throw new FileNotFoundException("Content file does not exist for audio sample.");
        }

        using var fileStream = cache.ContentFileRead(path);
        var audioManager = dependencies.Resolve<IAudioInternal>();
        if (path.Extension == "ogg")
        {
            AudioStream = audioManager.LoadAudioOggVorbis(fileStream, path.ToString());
        }
        else if (path.Extension == "wav")
        {
            AudioStream = audioManager.LoadAudioWav(fileStream, path.ToString());
        }
        else
        {
            throw new NotSupportedException("Unable to load audio files outside of ogg Vorbis or PCM wav");
        }
    }

    public override void Reload(IDependencyCollection dependencies, ResPath path, CancellationToken ct = default)
    {
        dependencies.Resolve<IAudioInternal>().Remove(AudioStream);
        Load(dependencies, path);
    }

    public AudioResource(AudioStream stream) : base()
    {
        AudioStream = stream;
    }

    public AudioResource() : base(){}

    public static implicit operator AudioStream(AudioResource res)
    {
        return res.AudioStream;
    }
}
