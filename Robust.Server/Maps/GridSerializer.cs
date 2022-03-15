using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Server.Maps
{
    [TypeSerializer]
    internal sealed class MapChunkSerializer : ITypeSerializer<MapChunk, MappingDataNode>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }

        public MapChunk Read(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null, MapChunk? chunk = null)
        {
            var tileNode = (ValueDataNode)node["tiles"];
            var tileBytes = Convert.FromBase64String(tileNode.Value);

            using var stream = new MemoryStream(tileBytes);
            using var reader = new BinaryReader(stream);

            var mapManager = dependencies.Resolve<IMapManager>();
            mapManager.SuppressOnTileChanged = true;

            if (chunk == null)
            {
                throw new InvalidOperationException(
                    $"Someone tried deserializing a gridchunk without passing a value.");
            }

            if (context is not MapLoader.MapContext mapContext)
            {
                throw new InvalidOperationException(
                    $"Someone tried serializing a gridchunk without passing {nameof(MapLoader.MapContext)} as context.");
            }

            var tileMap = mapContext.TileMap;

            if (tileMap == null)
            {
                throw new InvalidOperationException(
                    $"Someone tried deserializing a gridchunk before deserializing the tileMap.");
            }

            chunk.SuppressCollisionRegeneration = true;

            var tileDefinitionManager = dependencies.Resolve<ITileDefinitionManager>();

            for (ushort y = 0; y < chunk.ChunkSize; y++)
            {
                for (ushort x = 0; x < chunk.ChunkSize; x++)
                {
                    var id = reader.ReadUInt16();
                    var flags = (TileRenderFlag)reader.ReadByte();
                    var variant = reader.ReadByte();

                    var defName = tileMap[id];
                    id = tileDefinitionManager[defName].TileId;

                    var tile = new Tile(id, flags, variant);
                    chunk.SetTile(x, y, tile);
                }
            }

            chunk.SuppressCollisionRegeneration = false;
            mapManager.SuppressOnTileChanged = false;

            return chunk;
        }

        public DataNode Write(ISerializationManager serializationManager, MapChunk value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var root = new MappingDataNode();
            var ind = new ValueDataNode($"{value.X},{value.Y}");
            root.Add("ind", ind);

            var gridNode = new ValueDataNode();
            root.Add("tiles", gridNode);

            gridNode.Value = SerializeTiles(value);

            return root;
        }

        private static string SerializeTiles(MapChunk chunk)
        {
            // number of bytes written per tile, because sizeof(Tile) is useless.
            const int structSize = 4;

            var nTiles = chunk.ChunkSize * chunk.ChunkSize * structSize;
            var barr = new byte[nTiles];

            using (var stream = new MemoryStream(barr))
            using (var writer = new BinaryWriter(stream))
            {
                for (ushort y = 0; y < chunk.ChunkSize; y++)
                {
                    for (ushort x = 0; x < chunk.ChunkSize; x++)
                    {
                        var tile = chunk.GetTile(x, y);
                        writer.Write(tile.TypeId);
                        writer.Write((byte)tile.Flags);
                        writer.Write(tile.Variant);
                    }
                }
            }

            return Convert.ToBase64String(barr);
        }

        public MapChunk Copy(ISerializationManager serializationManager, MapChunk source, MapChunk target, bool skipHook,
            ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }
    }

    //todo paul make this be used
    [TypeSerializer]
    internal sealed class GridSerializer : ITypeSerializer<MapGrid, MappingDataNode>
    {

        public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }

        public MapGrid Read(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null, MapGrid? grid = null)
        {
            var info = node.Get<MappingDataNode>("settings");
            var chunks = node.Get<SequenceDataNode>("chunks");
            ushort csz = 0;
            ushort tsz = 0;
            float sgsz = 0.0f;

            foreach (var kvInfo in info.Cast<KeyValuePair<ValueDataNode, ValueDataNode>>())
            {
                var key = kvInfo.Key.Value;
                var val = kvInfo.Value.Value;
                if (key == "chunksize")
                    csz = ushort.Parse(val);
                else if (key == "tilesize")
                    tsz = ushort.Parse(val);
                else if (key == "snapsize")
                    sgsz = float.Parse(val, CultureInfo.InvariantCulture);
            }

            //TODO: Pass in options
            if (context is not MapLoader.MapContext mapContext)
            {
                throw new InvalidOperationException(
                    $"Someone tried serializing a mapgrid without passing {nameof(MapLoader.MapContext)} as context.");
            }

            if (grid == null) throw new NotImplementedException();
            //todo paul grid ??= dependencies.Resolve<MapManager>().CreateUnboundGrid(mapContext.TargetMap);

            foreach (var chunkNode in chunks.Cast<MappingDataNode>())
            {
                var (chunkOffsetX, chunkOffsetY) =
                    serializationManager.Read<Vector2i>(chunkNode["ind"], context, skipHook);
                var chunk = grid.GetChunk(chunkOffsetX, chunkOffsetY);
                serializationManager.Read(typeof(MapChunkSerializer), chunkNode, context, skipHook, chunk);
            }

            return grid;
        }

        public DataNode Write(ISerializationManager serializationManager, MapGrid value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var gridn = new MappingDataNode();
            var info = new MappingDataNode();
            var chunkSeq = new SequenceDataNode();

            gridn.Add("settings", info);
            gridn.Add("chunks", chunkSeq);

            info.Add("chunksize", value.ChunkSize.ToString(CultureInfo.InvariantCulture));
            info.Add("tilesize", value.TileSize.ToString(CultureInfo.InvariantCulture));

            var chunks = value.GetMapChunks();
            foreach (var chunk in chunks)
            {
                var chunkNode = serializationManager.WriteValue(chunk.Value);
                chunkSeq.Add(chunkNode);
            }

            return gridn;
        }

        public MapGrid Copy(ISerializationManager serializationManager, MapGrid source, MapGrid target, bool skipHook,
            ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }
    }
}
