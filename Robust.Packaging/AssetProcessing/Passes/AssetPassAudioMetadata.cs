using System.Globalization;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Packaging.AssetProcessing.Passes;

/// <summary>
/// Strips out audio files and writes them to a metadata .yml
/// Used for server packaging to avoid bundling entire audio files on the server.
/// </summary>
public sealed class AssetPassAudioMetadata : AssetPass
{
    private string[] _audioExtensions = new[]
    {
        ".ogg",
        ".wav",
    };

    private List<AudioMetadataPrototype> _audioMetadata = new();
    private readonly string _metadataPath;

    private SharedAudioManager _audioManager;

    public bool Enabled = false;

    public AssetPassAudioMetadata(string metadataPath = "Resources/Prototypes/audio_metadata.yml")
    {
        _metadataPath = metadataPath;
        _audioManager = new HeadlessAudioManager();
    }

    protected override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        if (!Enabled)
            return AssetFileAcceptResult.Pass;

        var ext = Path.GetExtension(file.Path);

        if (!_audioExtensions.Contains(ext))
            return AssetFileAcceptResult.Pass;

        var updatedName = file.Path.Replace("/", "_");

        if (updatedName.StartsWith("Resources_"))
            updatedName = updatedName[10..];

        TimeSpan length;

        if (ext == ".ogg")
        {
            using var stream = file.Open();
            var vorbis = _audioManager.LoadAudioOggVorbis(stream);
            length = vorbis.Length;
        }
        else if (ext == ".wav")
        {
            using var stream = file.Open();
            var vorbis = _audioManager.LoadAudioWav(stream);
            length = vorbis.Length;
        }
        else
        {
            throw new NotImplementedException($"No audio metadata processing implemented for {ext}");
        }

        _audioMetadata.Add(new AudioMetadataPrototype()
        {
            ID = updatedName,
            Length = length,
        });

        return AssetFileAcceptResult.Consumed;
    }

    protected override void AcceptFinished()
    {
        if (_audioMetadata.Count == 0)
            return;

        // ReSharper disable once InconsistentlySynchronizedField
        var root = new YamlSequenceNode();
        var document = new YamlDocument(root);

        foreach (var prototype in _audioMetadata)
        {
            // TODO: I know but sermanager and please get me out of this hell.
            var jaml = new YamlMappingNode
            {
                { "type", AudioMetadataPrototype.ProtoName },
                { "id", new YamlScalarNode(prototype.ID) },
                { "length", new YamlScalarNode(prototype.Length.TotalSeconds.ToString(CultureInfo.InvariantCulture)) }
            };
            root.Add(jaml);
        }

        RunJob(() =>
        {
            using var memory = new MemoryStream();
            using var writer = new StreamWriter(memory);
            var yamlStream = new YamlStream(document);
            yamlStream.Save(new YamlNoDocEndDotsFix(new YamlMappingFix(new Emitter(writer))), false);
            writer.Flush();
            var result = new AssetFileMemory(_metadataPath, memory.ToArray());
            SendFile(result);
        });
    }
}
