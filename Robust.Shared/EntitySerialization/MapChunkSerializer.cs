using System;
using System.Collections.Generic;
using System.IO;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.EntitySerialization;

[TypeSerializer]
internal sealed class MapChunkSerializer : ITypeSerializer<MapChunk, MappingDataNode>, ITypeCopyCreator<MapChunk>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }

    public MapChunk Read(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<MapChunk>? instantiationDelegate = null)
    {
        var ind = (Vector2i) serializationManager.Read(typeof(Vector2i), node["ind"], hookCtx, context)!;
        var tileNode = (ValueDataNode)node["tiles"];
        var tileBytes = Convert.FromBase64String(tileNode.Value);

        using var stream = new MemoryStream(tileBytes);
        using var reader = new BinaryReader(stream);

        var mapManager = dependencies.Resolve<IMapManager>();
        mapManager.SuppressOnTileChanged = true;

        ushort size = 16;

        // TODO: This should be on the context I think?
        if (node.TryGet("size", out ValueDataNode? sizeNode))
        {
            size = (ushort) serializationManager.Read(typeof(ushort), sizeNode, context)!;
        }

        var chunk = instantiationDelegate != null ? instantiationDelegate() : new MapChunk(ind.X, ind.Y, size);

        IReadOnlyDictionary<int, string>? tileMap = null;

        if (context is EntityDeserializer serContext)
            tileMap = serContext.TileMap;

        if (tileMap == null)
        {
            throw new InvalidOperationException(
                $"Someone tried deserializing a gridchunk before deserializing the tileMap.");
        }

        chunk.SuppressCollisionRegeneration = true;

        var tileDefinitionManager = dependencies.Resolve<ITileDefinitionManager>();

        node.TryGetValue("version", out var versionNode);
        var version = ((ValueDataNode?) versionNode)?.AsInt() ?? 1;

        for (ushort y = 0; y < chunk.ChunkSize; y++)
        {
            for (ushort x = 0; x < chunk.ChunkSize; x++)
            {
                var id = version < 6 ? reader.ReadUInt16() : reader.ReadInt32();
                var flags = reader.ReadByte();
                var variant = reader.ReadByte();

                var defName = tileMap[id];
                id = tileDefinitionManager[defName].TileId;

                var tile = new Tile(id, flags, variant);
                chunk.TrySetTile(x, y, tile, out _, out _);
            }
        }

        chunk.SuppressCollisionRegeneration = false;
        mapManager.SuppressOnTileChanged = false;

        return chunk;
    }

    public DataNode Write(ISerializationManager serializationManager, MapChunk value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        DebugTools.Assert(value.FilledTiles > 0, "Attempting to write an empty chunk");
        var root = new MappingDataNode();
        var ind = new ValueDataNode($"{value.X},{value.Y}");
        root.Add("ind", ind);

        var gridNode = new ValueDataNode();
        root.Add("tiles", gridNode);

        root.Add("version", new ValueDataNode("6"));

        gridNode.Value = SerializeTiles(value, context as EntitySerializer);

        return root;
    }

    private static string SerializeTiles(MapChunk chunk, EntitySerializer? serializer)
    {
        // number of bytes written per tile, because sizeof(Tile) is useless.
        const int structSize = 6;

        var nTiles = chunk.ChunkSize * chunk.ChunkSize * structSize;
        var barr = new byte[nTiles];

        using (var stream = new MemoryStream(barr))
        using (var writer = new BinaryWriter(stream))
        {
            if (serializer == null)
            {
                for (ushort y = 0; y < chunk.ChunkSize; y++)
                {
                    for (ushort x = 0; x < chunk.ChunkSize; x++)
                    {
                        var tile = chunk.GetTile(x, y);
                        writer.Write(tile.TypeId);
                        writer.Write((byte) tile.Flags);
                        writer.Write(tile.Variant);
                    }
                }
                return Convert.ToBase64String(barr);
            }

            var lastTile = -1;
            var yamlId = -1;
            for (ushort y = 0; y < chunk.ChunkSize; y++)
            {
                for (ushort x = 0; x < chunk.ChunkSize; x++)
                {
                    var tile = chunk.GetTile(x, y);
                    if (tile.TypeId != lastTile)
                        yamlId = serializer.GetYamlTileId(tile.TypeId);

                    lastTile = tile.TypeId;
                    writer.Write(yamlId);
                    writer.Write((byte) tile.Flags);
                    writer.Write(tile.Variant);
                }
            }
        }

        return Convert.ToBase64String(barr);
    }

    public MapChunk CreateCopy(
        ISerializationManager serializationManager,
        MapChunk source,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        var mapManager = dependencies.Resolve<IMapManager>();
        mapManager.SuppressOnTileChanged = true;
        var chunk = new MapChunk(source.X, source.Y, source.ChunkSize)
        {
            SuppressCollisionRegeneration = true
        };

        for (ushort y = 0; y < chunk.ChunkSize; y++)
        {
            for (ushort x = 0; x < chunk.ChunkSize; x++)
            {
                chunk.TrySetTile(x, y, source.GetTile(x, y), out _, out _);
            }
        }

        mapManager.SuppressOnTileChanged = false;
        chunk.SuppressCollisionRegeneration = false;

        return chunk;
    }
}
