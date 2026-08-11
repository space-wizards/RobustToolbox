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
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using SharpZstd.Interop;

namespace Robust.Shared.GameSaves;

public sealed partial class GameSavesSystem : EntitySystem
{
    [Dependency] private IResourceManager _resourceManager = default!;
    [Dependency] private IRobustSerializer _serializer = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;

    /// <summary>
    /// File extension that represents a ZSTD compressed YAML file with a single mapping data node.
    /// </summary>
    public const string Extension = ".rtsave";

    private bool _enabled;

    private ZStdCompressionContext _zstdContext = default!;

    public override void Initialize()
    {
        base.Initialize();
        _zstdContext = new ZStdCompressionContext();
        _zstdContext.SetParameter(ZSTD_cParameter.ZSTD_c_compressionLevel, _config.GetCVar(CVars.GameSavesCompressLevel));
        Subs.CVar(_config, CVars.GameSavesEnabled, value => _enabled = value, true);
    }

    /// <summary>
    /// Serializes all entities and compresses the resulting data into a save file.
    /// </summary>
    /// <param name="path">Path to a save file. The extension is always ignored.</param>
    /// <returns>True if the game was saved successfully, false if saves are disabled
    /// or an error occured during entity serialization.</returns>
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

        // TODO add support for uncompressed saves (.yml file extension)
        if (path.Extension == Extension)
        {
            if (!TryReadCompressedZstd(path, out var data))
                return false;

            if (!_mapLoader.TryLoadGeneric(data, path.Filename, out _))
                return false;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Compresses a YAML data node using ZSTD compression.
    /// </summary>
    /// <param name="path">Path to a file.</param>
    /// <param name="data">Mapping data node to compress in the specified path.</param>
    private void WriteCompressedZstd(ResPath path, MappingDataNode data)
    {
        using var uncompressedStream = new MemoryStream();
        _serializer.SerializeDirect(uncompressedStream, data.ToString());

        if (!uncompressedStream.TryGetBuffer(out var uncompressed))
        {
            uncompressed = new ArraySegment<byte>(uncompressedStream.ToArray());
        }

        byte[]? buf = null;
        try
        {
            var bound = ZStd.CompressBound(uncompressed.Count);
            buf = ArrayPool<byte>.Shared.Rent(4 + bound);

            // Write the uncompressed length into the first 4 bytes
            BitConverter.TryWriteBytes(buf.AsSpan(0, 4), uncompressed.Count);

            var compressedLength = _zstdContext.Compress2(
                buf.AsSpan(4, bound),
                uncompressed.AsSpan());

            Log.Info($"Saving serialized results to {path}");
            path = path.ToRootedPath();
            _resourceManager.UserData.CreateDir(path.Directory);

            using var writer = _resourceManager.UserData.OpenWrite(path);
            writer.Write(buf, 0, 4 + compressedLength);
        }
        finally
        {
            if (buf != null)
                ArrayPool<byte>.Shared.Return(buf);
        }
    }

    private bool TryReadCompressedZstd(ResPath path, [NotNullWhen(true)] out Stream? data)
    {
        data = null;
        var intBuf = new byte[4];

        using var fileStream = _resourceManager.ContentFileRead(path);

        // Read the prefix
        fileStream.ReadExactly(intBuf);
        var uncompressedSize = BitConverter.ToInt32(intBuf);

        using var decompressStream = new ZStdDecompressStream(fileStream, false);

        using var decompressedStream = new MemoryStream(uncompressedSize);
        decompressStream.CopyTo(decompressedStream);
        decompressedStream.Position = 0;

        DebugTools.Assert(uncompressedSize == decompressedStream.Length);

        while (decompressedStream.Position < decompressedStream.Length)
        {
            data = decompressedStream;
            return true;
        }

        return false;
    }
}
