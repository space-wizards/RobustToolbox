using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IResourceManager _resourceManager = default!;
    [Dependency] private IDependencyCollection _dependency = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    /// <summary>
    /// File extension that represents a compressed save file of entities.
    /// The data inside is a ZStd compressed YAML file with a single mapping data node.
    /// </summary>
    public const string SaveExtension = "rtsave";

    private ZStdCompressionContext _zStdContext = default!;

    public override void Initialize()
    {
        base.Initialize();
        _zStdContext = new ZStdCompressionContext();
        _zStdContext.SetParameter(ZSTD_cParameter.ZSTD_c_compressionLevel, _config.GetCVar(CVars.MapSavesCompressLevel));
    }

    /// <summary>
    /// Generic method that writes a YAML data node into a file.
    /// If the file has a special extension, the contents will be compressed using ZSTD,
    /// otherwise it will be written as plain text.
    /// </summary>
    /// <param name="path">Path to a file.</param>
    /// <param name="data">Mapping data node to write.</param>
    public void Write(ResPath path, MappingDataNode data)
    {
        Log.Info($"Saving serialized results to {path}");

        var stopwatch = new RStopwatch();
        stopwatch.Start();

        path = path.ToRootedPath();
        _resourceManager.UserData.CreateDir(path.Directory);

        if (path.Extension == SaveExtension)
        {
            using var writer = _resourceManager.UserData.OpenWrite(path);
            WriteCompressedZStd(writer, data, _zStdContext);
        }
        else
        {
            using var writer = _resourceManager.UserData.OpenWriteText(path);
            WriteYaml(writer, data);
        }

        Log.Info($"Saved serialized results to {path} in {stopwatch.Elapsed}");
    }

    /// <summary>
    /// Writes a YAML data node into a file as plain text.
    /// </summary>
    /// <param name="target">The target text writer.</param>
    /// <param name="data">Mapping data node to write into the specified text writer.</param>
    private static void WriteYaml(TextWriter target, MappingDataNode data)
    {
        var document = new YamlDocument(data.ToYaml());
        var stream = new YamlStream {document};
        stream.Save(new YamlMappingFix(new Emitter(target)), false);
    }

    /// <summary>
    /// Compresses a YAML data node using ZStd compression.
    /// </summary>
    /// <param name="target">Target stream to write in.</param>
    /// <param name="data">Mapping data node to compress into the specified path.</param>
    /// <param name="zStdContext">ZStd context to use for compression.</param>
    private static void WriteCompressedZStd(Stream target, MappingDataNode data, ZStdCompressionContext zStdContext)
    {
        using var uncompressedStream = new MemoryStream(Encoding.UTF8.GetBytes(data.ToString()));

        if (!uncompressedStream.TryGetBuffer(out var uncompressed))
        {
            uncompressed = new ArraySegment<byte>(uncompressedStream.ToArray());
        }

        var bound = ZStd.CompressBound(uncompressed.Count);
        var buf = ArrayPool<byte>.Shared.Rent(4 + bound);
        try
        {
            // Write the uncompressed length into the first 4 bytes
            BitConverter.TryWriteBytes(buf.AsSpan(0, 4), uncompressed.Count);

            var compressedLength = zStdContext.Compress2(
                buf.AsSpan(4, bound),
                uncompressed.AsSpan());

            target.Write(buf, 0, 4 + compressedLength);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    public bool TryReadFile(ResPath file, [NotNullWhen(true)] out MappingDataNode? data)
    {
        var resPath = file.ToRootedPath();
        data = null;

        if (file.Extension == SaveExtension)
        {
            if (!TryGetStream(file, out var fileStream))
                return false;

            Log.Info($"Loading file: {resPath}");
            return TryReadCompressedFile(fileStream, out data);
        }

        if (!TryGetReader(resPath, out var reader))
            return false;

        Log.Info($"Loading file: {resPath}");
        return TryReadFile(reader, out data);
    }

    private static bool TryReadFile(TextReader reader, [NotNullWhen(true)] out MappingDataNode? data, ISawmill? log = null)
    {
        data = null;

        var stopwatch = new RStopwatch();
        stopwatch.Start();

        using var textReader = reader;
        var documents = DataNodeParser.ParseYamlStream(reader).ToArray();
        log?.Debug($"Loaded yml stream in {stopwatch.Elapsed}");

        // Yes, logging errors in a "try" method is kinda shit, but it was throwing exceptions when I found it and it does
        // make sense to at least provide some kind of feedback for why it failed.
        switch (documents.Length)
        {
            case < 1:
                log?.Error("Stream has no YAML documents.");
                return false;
            case > 1:
                log?.Error("Stream too many YAML documents. Map files store exactly one.");
                return false;
            default:
                data = (MappingDataNode) documents[0].Root;
                return true;
        }
    }

    /// <summary>
    /// Tries to read a ZSTD compressed save file from a file path.
    /// </summary>
    /// <param name="fileStream">The target file stream to decompress and read.</param>
    /// <param name="data">The decompressed map data.</param>
    /// <returns>True if the file was read successfully.</returns>
    private static bool TryReadCompressedFile(Stream fileStream, [NotNullWhen(true)] out MappingDataNode? data)
    {
        data = null;
        var intBuf = new byte[4];

        using (fileStream)
        {
            fileStream.ReadExactly(intBuf);
            var uncompressedSize = BitConverter.ToInt32(intBuf);

            using var decompressStream = new ZStdDecompressStream(fileStream, false);

            using var decompressedStream = new MemoryStream(uncompressedSize);
            decompressStream.CopyTo(decompressedStream);
            decompressedStream.Position = 0;

            DebugTools.Assert(uncompressedSize == decompressedStream.Length);

            using var reader = new StreamReader(decompressedStream);
            return TryReadFile(reader, out data);
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
