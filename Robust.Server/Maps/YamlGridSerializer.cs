using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Server.Maps
{
    internal sealed class GridChunk : ITypeSerializer<MapChunk, MappingDataNode>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }

        public DeserializationResult Read(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null)
        {
            throw new NotImplementedException();
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

        private static string SerializeTiles(IMapChunk chunk)
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
                        writer.Write(tile.Data);
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

    internal class GridSerializer : ITypeSerializer<MapGrid, MappingDataNode>
    {
        private static void DeserializeChunk(IMapManager mapMan, IMapGridInternal grid, YamlMappingNode chunkData, IReadOnlyDictionary<ushort, string> tileDefMapping, ITileDefinitionManager tileDefinitionManager)
        {
            var indNode = chunkData["ind"];
            var tileNode = chunkData["tiles"];

            var (chunkOffsetX, chunkOffsetY) = indNode.AsVector2i();
            var tileBytes = Convert.FromBase64String(tileNode.ToString());

            using var stream = new MemoryStream(tileBytes);
            using var reader = new BinaryReader(stream);

            mapMan.SuppressOnTileChanged = true;

            var chunk = grid.GetChunk(chunkOffsetX, chunkOffsetY);

            chunk.SuppressCollisionRegeneration = true;

            for (ushort y = 0; y < grid.ChunkSize; y++)
            {
                for (ushort x = 0; x < grid.ChunkSize; x++)
                {
                    var id = reader.ReadUInt16();
                    var data = reader.ReadUInt16();

                    var defName = tileDefMapping[id];
                    id = tileDefinitionManager[defName].TileId;

                    var tile = new Tile(id, data);
                    chunk.SetTile(x, y, tile);
                }
            }

            chunk.SuppressCollisionRegeneration = false;
            mapMan.SuppressOnTileChanged = false;
        }

        public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }
        public static MapGrid DeserializeGrid(IMapManagerInternal mapMan, MapId mapId, YamlMappingNode info,
            YamlSequenceNode chunks, IReadOnlyDictionary<ushort, string> tileDefMapping,
            ITileDefinitionManager tileDefinitionManager)
        {
        }
        public DeserializationResult Read(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null)
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
                    $"Someone serializing a mapgrid without passing {nameof(MapLoader.MapContext)} as context.");
            }
            var grid = dependencies.Resolve<MapManager>().CreateUnboundGrid(mapContext.TargetMap);

            foreach (var chunkNode in chunks.Cast<YamlMappingNode>())
            {
                DeserializeChunk(mapMan, grid, chunkNode, tileDefMapping, tileDefinitionManager);
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
