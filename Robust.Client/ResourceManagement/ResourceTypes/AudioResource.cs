using System;
using System.IO;
using System.Linq;
using System.Threading;
using Robust.Client.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement;

public sealed class AudioResource : BaseResource
{
    // from: https://en.wikipedia.org/wiki/List_of_file_signatures
    private static readonly byte[] OggSignature = "OggS"u8.ToArray();
    private static readonly byte[] RiffSignature = "RIFF"u8.ToArray();
    private const int WavSignatureStart = 8; // RIFF????
    private static readonly byte[] WavSignature = "WAVE"u8.ToArray();
    private const int MaxSignatureLength = 12; // RIFF????WAVE

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
        var seekableStream = fileStream.CanSeek ? fileStream : fileStream.CopyToMemoryStream();
        byte[] signature = seekableStream.ReadExact(MaxSignatureLength);
        seekableStream.Seek(0, SeekOrigin.Begin);

        var audioManager = dependencies.Resolve<IAudioInternal>();
        if (signature[..OggSignature.Length].SequenceEqual(OggSignature))
        {
            AudioStream = audioManager.LoadAudioOggVorbis(seekableStream, path.ToString());
        }
        else if (signature[..RiffSignature.Length].SequenceEqual(RiffSignature)
                 && signature[WavSignatureStart..MaxSignatureLength].SequenceEqual(WavSignature))
        {
            AudioStream = audioManager.LoadAudioWav(seekableStream, path.ToString());
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
