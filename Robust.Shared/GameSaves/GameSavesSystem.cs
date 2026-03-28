using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.GameSaves;

/// <summary>
/// Saves and loads the full game state (all entities) to/from .rtsave files (ZSTD-compressed YAML).
/// </summary>
public sealed class GameSavesSystem : EntitySystem
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    /// <summary>
    /// File extension for ZSTD-compressed YAML game save files.
    /// </summary>
    public const string Extension = ".rtsave";

    private bool _enabled;
    private ZStdCompressionContext? _zstdContext;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_config, CVars.GameSavesEnabled, value => _enabled = value, true);
    }

    public override void Shutdown()
    {
        _zstdContext?.Dispose();
        _zstdContext = null;
        base.Shutdown();
    }

    public bool TrySaveGame(ResPath path)
    {
        if (!_enabled)
            return false;

        var ev = new BeforeGameSaveEvent(path);
        RaiseLocalEvent(ref ev);

        if (!_mapLoader.TrySerializeAllEntities(out var data))
            return false;

        WriteCompressedZstd(path, data);
        return true;
    }

    public bool TryLoadGame(ResPath path)
    {
        if (!_enabled)
            return false;

        var ev = new BeforeGameLoadEvent(path);
        RaiseLocalEvent(ref ev);

        if (!TryReadCompressedZstd(path, out var data))
            return false;

        var opts = MapLoadOptions.Default with { ExpectedCategory = FileCategory.Save };
        if (!_mapLoader.TryLoadGeneric(data, path.ToString(), out _, opts))
            return false;

        return true;
    }

    private void WriteCompressedZstd(ResPath path, MappingDataNode data)
    {
        var yamlString = MappingDataNodeToYamlString(data);
        var uncompressed = Encoding.UTF8.GetBytes(yamlString);

        _zstdContext ??= new ZStdCompressionContext();
        _zstdContext.SetParameter(SharpZstd.Interop.ZSTD_cParameter.ZSTD_c_compressionLevel,
            _config.GetCVar(CVars.GameSavesCompressLevel));

        var pathWithExt = path.ToString().EndsWith(Extension, StringComparison.OrdinalIgnoreCase)
            ? path
            : new ResPath(path.ToString() + Extension);
        pathWithExt = pathWithExt.ToRootedPath();
        _resourceManager.UserData.CreateDir(pathWithExt.Directory);

        var bound = ZStd.CompressBound(uncompressed.Length);
        var buf = ArrayPool<byte>.Shared.Rent(4 + bound);
        try
        {
            var compressedLength = _zstdContext.Compress2(
                buf.AsSpan(4, bound),
                uncompressed.AsSpan());

            if (!BitConverter.TryWriteBytes(buf.AsSpan(0, 4), uncompressed.Length))
                return;

            using var stream = _resourceManager.UserData.OpenWrite(pathWithExt);
            stream.Write(buf.AsSpan(0, 4 + compressedLength));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    private bool TryReadCompressedZstd(ResPath path, [NotNullWhen(true)] out MappingDataNode? data)
    {
        data = null;
        var pathWithExt = path.ToString().EndsWith(Extension, StringComparison.OrdinalIgnoreCase)
            ? path
            : new ResPath(path.ToString() + Extension);
        pathWithExt = pathWithExt.ToRootedPath();
        if (!_resourceManager.UserData.Exists(pathWithExt))
            return false;

        using var fileStream = _resourceManager.UserData.OpenRead(pathWithExt);
        var lengthBuf = new byte[4];
        if (fileStream.Read(lengthBuf, 0, 4) != 4)
            return false;

        var uncompressedSize = BitConverter.ToInt32(lengthBuf);
        if (uncompressedSize <= 0 || uncompressedSize > 100 * 1024 * 1024)
            return false;

        using var decompressStream = new ZStdDecompressStream(fileStream, ownStream: false);
        using var decompressed = new MemoryStream(uncompressedSize);
        decompressStream.CopyTo(decompressed);
        decompressed.Position = 0;

        if (decompressed.Length != uncompressedSize)
            return false;

        using var reader = new StreamReader(decompressed, Encoding.UTF8);
        foreach (var document in DataNodeParser.ParseYamlStream(reader))
        {
            data = (MappingDataNode)document.Root;
            return true;
        }

        return false;
    }

    private static string MappingDataNodeToYamlString(MappingDataNode node)
    {
        using var writer = new StringWriter();
        var document = new YamlDocument(node.ToYaml());
        var stream = new YamlStream { document };
        stream.Save(new YamlMappingFix(new Emitter(writer)), false);
        return writer.ToString();
    }
}
