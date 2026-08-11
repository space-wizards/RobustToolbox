using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SharpZstd.Interop;
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
    [Dependency] private IRobustSerializer _serializer = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IResourceManager _resourceManager = default!;
    [Dependency] private IDependencyCollection _dependency = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    /// <summary>
    /// File extension that represents a ZSTD compressed YAML file with a single mapping data node.
    /// </summary>
    public const string SaveExtension = "rtsave";

    private ZStdCompressionContext _zstdContext = default!;

    public override void Initialize()
    {
        base.Initialize();
        _zstdContext = new ZStdCompressionContext();
        _zstdContext.SetParameter(ZSTD_cParameter.ZSTD_c_compressionLevel, _config.GetCVar(CVars.MapSavesCompressLevel));
    }

    /// <summary>
    /// Writes a YAML data node into a file as plain text.
    /// </summary>
    /// <param name="target">The target text writer.</param>
    /// <param name="data">Mapping data node to write into the specified text writer.</param>
    private void WriteYaml(TextWriter target, MappingDataNode data)
    {
        var document = new YamlDocument(data.ToYaml());
        var stream = new YamlStream {document};
        stream.Save(new YamlMappingFix(new Emitter(target)), false);
    }

    /// <summary>
    /// Gets the text writer for a specified path.
    /// </summary>
    /// <param name="path">The target path.</param>
    /// <returns>The text writer for that path.</returns>
    private StreamWriter GetWriterForPath(ResPath path)
    {
        Log.Info($"Saving serialized results to {path}");
        path = path.ToRootedPath();
        _resourceManager.UserData.CreateDir(path.Directory);
        return _resourceManager.UserData.OpenWriteText(path);
    }

    /// <summary>
    /// Compresses a YAML data node using ZSTD compression.
    /// </summary>
    /// <param name="path">Path to a file.</param>
    /// <param name="data">Mapping data node to compress into the specified path.</param>
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

    /// <summary>
    /// Generic method that writes a YAML data node into a file.
    /// If the file has a special extension, the contents will be compressed using ZSTD,
    /// otherwise it will be written as plain text.
    /// </summary>
    /// <param name="path">Path to a file.</param>
    /// <param name="data">Mapping data node to write.</param>
    private void Write(ResPath path, MappingDataNode data)
    {
        Log.Info($"Saving serialized results to {path}");
        path = path.ToRootedPath();
        _resourceManager.UserData.CreateDir(path.Directory);

        if (path.Extension == SaveExtension)
        {
            WriteCompressedZstd(path, data);
        }
        else
        {
            using var writer = _resourceManager.UserData.OpenWriteText(path);
            WriteYaml(writer, data);
        }
    }

    public bool TryReadFile(ResPath file, [NotNullWhen(true)] out MappingDataNode? data)
    {
        var resPath = file.ToRootedPath();
        data = null;

        if (file.Extension == SaveExtension)
            return TryReadCompressedFile(file, out data);

        if (!TryGetReader(resPath, out var reader))
            return false;

        Log.Info($"Loading file: {resPath}");
        return TryReadFile(reader, out data);
    }

    private bool TryReadFile(TextReader reader, [NotNullWhen(true)] out MappingDataNode? data)
    {
        data = null;

        var stopwatch = new RStopwatch();
        stopwatch.Start();

        using var textReader = reader;
        var documents = DataNodeParser.ParseYamlStream(reader).ToArray();
        Log.Debug($"Loaded yml stream in {stopwatch.Elapsed}");

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

    private bool TryGetReader(ResPath resPath, [NotNullWhen(true)] out TextReader? reader)
    {
        if (_resourceManager.UserData.Exists(resPath))
        {
            // Log warning if file exists in both user and content data.
            if (_resourceManager.ContentFileExists(resPath))
                Log.Warning("Reading map user data instead of content");

            reader = _resourceManager.UserData.OpenText(resPath);
            return true;
        }

        if (_resourceManager.TryContentFileRead(resPath, out var contentReader))
        {
            reader = new StreamReader(contentReader);
            return true;
        }

        Log.Error($"File not found: {resPath}");
        reader = null;
        return false;
    }

    private bool TryGetStream(ResPath resPath, [NotNullWhen(true)] out Stream? stream)
    {
        if (_resourceManager.UserData.Exists(resPath))
        {
            // Log warning if file exists in both user and content data.
            if (_resourceManager.ContentFileExists(resPath))
                Log.Warning("Reading map user data instead of content");

            stream = _resourceManager.UserData.OpenRead(resPath);
            return true;
        }

        if (_resourceManager.TryContentFileRead(resPath, out stream))
            return true;

        Log.Error($"File not found: {resPath}");
        stream = null;
        return false;
    }

    /// <summary>
    /// Tries to read a ZSTD compressed save file from a file path.
    /// </summary>
    /// <param name="path">The target file to decompress and read.</param>
    /// <param name="data">The decompressed map data.</param>
    /// <returns>True if the file was read successfully.</returns>
    private bool TryReadCompressedFile(ResPath path, [NotNullWhen(true)] out MappingDataNode? data)
    {
        data = null;
        var intBuf = new byte[4];

        if (!TryGetStream(path, out var fileStream))
            return false;

        using (fileStream)
        {
            fileStream.ReadExactly(intBuf);
            var uncompressedSize = BitConverter.ToInt32(intBuf);

            using var decompressStream = new ZStdDecompressStream(fileStream, false);

            using var decompressedStream = new MemoryStream(uncompressedSize);
            decompressStream.CopyTo(decompressedStream);
            decompressedStream.Position = 0;

            DebugTools.Assert(uncompressedSize == decompressedStream.Length);

            // Some robust serializer shenanigans add 5 bytes of garbage to our stream, so we have to skip it manually.
            decompressedStream.Position = 9;

            return TryReadFile(new StreamReader(decompressedStream, leaveOpen: true), out data);
        }
    }

    /// <summary>
    /// Helper method for deleting all loaded entities.
    /// </summary>
    public void Delete(LoadResult result)
    {
        foreach (var uid in result.Maps)
        {
            Del(uid);
        }

        foreach (var uid in result.Orphans)
        {
            Del(uid);
        }

        foreach (var uid in result.Entities)
        {
            Del(uid);
        }

        foreach (var uid in result.NullspaceEntities)
        {
            Del(uid);
        }
    }
}
