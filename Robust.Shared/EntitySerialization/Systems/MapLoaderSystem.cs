using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.EntitySerialization.Systems;

/// <summary>
/// This class provides methods for saving and loading maps and grids.
/// </summary>
/// <remarks>
/// The save & load methods are basically wrappers around <see cref="EntitySerializer"/> and
/// <see cref="EntityDeserializer"/>, which can be used for more control over serialization.
/// </remarks>
public sealed partial class MapLoaderSystem : EntitySystem
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IDependencyCollection _dependency = default!;

    private Stopwatch _stopwatch = new();

    private EntityQuery<MapComponent> _mapQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        base.Initialize();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _mapQuery = GetEntityQuery<MapComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
    }

    private void Write(ResPath path, MappingDataNode data)
    {
        var document = new YamlDocument(data.ToYaml());
        using var writer = _resourceManager.UserData.OpenWriteText(path);
        {
            var stream = new YamlStream {document};
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);
        }
    }

    private bool TryReadFile(ResPath file, [NotNullWhen(true)] out MappingDataNode? data)
    {
        var resPath = file.ToRootedPath();
        data = null;

        if (!TryGetReader(resPath, out var reader))
            return false;

        Log.Info($"Loading file: {resPath}");
        _stopwatch.Restart();

        using var textReader = reader;
        var documents = DataNodeParser.ParseYamlStream(reader).ToArray();
        Log.Debug($"Loaded yml stream in {_stopwatch.Elapsed}");

        // Yes, logging errors in a "try" method is kinda shit, but it was throwing exceptions when I found it and it does
        // make sense to at least provide some kind of feedback for why it failed.
        switch (documents.Length)
        {
            case < 1:
                Log.Error("Stream has no YAML documents.");
                return false;
            case > 1:
                Log.Error("Stream too many YAML documents. Map files store exactly one.");
                return false;
            default:
                data = (MappingDataNode) documents[0].Root;
                return true;
        }
    }
}
