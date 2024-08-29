using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Robust.Shared.Audio;
using Robust.Shared.Audio.AudioLoading;
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
    private readonly List<AudioMetadataPrototype> _audioMetadata = new();
    private readonly string _metadataPath;

    public AssetPassAudioMetadata(string metadataPath = "Prototypes/_audio_metadata.yml")
    {
        _metadataPath = metadataPath;
    }

    protected override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        if (!AudioLoader.IsLoadableAudioFile(file.Path))
            return AssetFileAcceptResult.Pass;

        using var stream = file.Open();
        var metadata = AudioLoader.LoadAudioMetadata(stream, file.Path);

        lock (_audioMetadata)
        {
            _audioMetadata.Add(new AudioMetadataPrototype()
            {
                ID = "/" + file.Path,
                Length = metadata.Length,
            });
        }

        return AssetFileAcceptResult.Consumed;
    }

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    protected override void AcceptFinished()
    {
        if (_audioMetadata.Count == 0)
        {
            Logger?.Debug("Have no audio metadata, not writing anything");
            return;
        }

        Logger?.Debug("Writing audio metadata for {0} audio files", _audioMetadata.Count);

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
