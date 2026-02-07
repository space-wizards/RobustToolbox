using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using SharpZstd.Interop;

namespace Robust.Shared.GameSaves;

public sealed class GameSavesSystem : EntitySystem
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IRobustSerializer _serializer = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    /// <summary>
    /// File extension that represents a ZSTD compressed YAML file with a single mapping data node.
    /// </summary>
    public const string Extension = ".rtsave";

    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();
        _zstdContext = new ZStdCompressionContext();
        _zstdContext.SetParameter(ZSTD_cParameter.ZSTD_c_compressionLevel, _config.GetCVar(CVars.GameSavesCompressLevel));
        Subs.CVar(_config, CVars.GameSavesEnabled, value => _enabled = value, true);
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

        if (!_mapLoader.TryLoadGeneric(data, path.Filename, out _))
            return false;

        return true;
    }

    private ZStdCompressionContext _zstdContext = default!;

    /// <summary>
    /// Compresses a YAML data node using ZSTD compression.
    /// </summary>
    /// <param name="path">Path to a file without a file extension</param>
    /// <param name="data">Mapping data node to compress in the specified path.</param>
    private void WriteCompressedZstd(ResPath path, MappingDataNode data)
    {
        var uncompressedStream = new MemoryStream();

        _serializer.SerializeDirect(uncompressedStream, MappingNodeToString(data));

        var uncompressed = uncompressedStream.AsSpan();
        var poolData = ArrayPool<byte>.Shared.Rent(uncompressed.Length);
        uncompressed.CopyTo(poolData);

        if (_resourceManager.UserData.RootDir == null)
            return; // can't save anything

        byte[]? buf = null;
        try
        {
            // Compress stream to buffer.
            // First 4 bytes of buffer are reserved for the length of the uncompressed stream.
            var bound = ZStd.CompressBound(uncompressed.Length);
            buf = ArrayPool<byte>.Shared.Rent(4 + bound);
            var compressedLength = _zstdContext.Compress2(
                buf.AsSpan(4, bound),
                poolData.AsSpan(0, uncompressed.Length));

            var filePath = Path.Combine(_resourceManager.UserData.RootDir, path.Filename + Extension);
            File.WriteAllBytes(filePath, buf);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(poolData);
            if (buf != null)
                ArrayPool<byte>.Shared.Return(buf);
        }
    }

    private bool TryReadCompressedZstd(ResPath path, [NotNullWhen(true)] out MappingDataNode? data)
    {
        data = null;

        var intBuf = new byte[4];

        using var fileStream = _resourceManager.ContentFileRead(path);
        using var decompressStream = new ZStdDecompressStream(fileStream, false);

        fileStream.ReadExactly(intBuf);
        var uncompressedSize = BitConverter.ToInt32(intBuf);

        var decompressedStream = new MemoryStream(uncompressedSize);
        decompressStream.CopyTo(decompressedStream);
        decompressedStream.Position = 0;
        DebugTools.Assert(uncompressedSize == decompressedStream.Length);

        while (decompressedStream.Position < decompressedStream.Length)
        {
            _serializer.DeserializeDirect<string>(decompressedStream, out var yml);
            if (!TryParseMappingNode(yml, out var node))
                return false;

            data = node;
            return true;
        }

        return false;
    }

    private string MappingNodeToString(MappingDataNode node)
    {
        return node.ToString();
    }

    private bool TryParseMappingNode(string yml, [NotNullWhen(true)] out MappingDataNode? node)
    {
        var stream = new StringReader(yml);
        foreach (var document in DataNodeParser.ParseYamlStream(stream))
        {
            node = (MappingDataNode) document.Root;
            return true;
        }

        node = null;
        return false;
    }
}
