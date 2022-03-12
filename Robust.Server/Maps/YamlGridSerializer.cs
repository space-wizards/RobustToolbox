using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Server.Maps
{
    internal static class YamlGridSerializer
    {
        public static YamlMappingNode SerializeGrid(IMapGrid mapGrid)
        {
            var grid = (IMapGridInternal) mapGrid;

            var gridn = new YamlMappingNode();
            var info = new YamlMappingNode();
            var chunkSeq = new YamlSequenceNode();

            gridn.Add("settings", info);
            gridn.Add("chunks", chunkSeq);

            info.Add("chunksize", grid.ChunkSize.ToString(CultureInfo.InvariantCulture));
            info.Add("tilesize", grid.TileSize.ToString(CultureInfo.InvariantCulture));

            var chunks = grid.GetMapChunks();
            foreach (var chunk in chunks)
            {
                var chunkNode = SerializeChunk(chunk.Value);
                chunkSeq.Add(chunkNode);
            }

            return gridn;
        }

        private static YamlNode SerializeChunk(MapChunk chunk)
        {
            var root = new YamlMappingNode();
            var value = new YamlScalarNode($"{chunk.X},{chunk.Y}");
            value.Style = ScalarStyle.DoubleQuoted;
            root.Add("ind", value);

            var gridNode = new YamlScalarNode();
            root.Add("tiles", gridNode);

            gridNode.Value = SerializeTiles(chunk);

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
                        writer.Write(tile.Data);
                    }
                }
            }

            return Convert.ToBase64String(barr);
        }

        public static MapGrid DeserializeMapGrid(IMapManagerInternal mapMan, YamlMappingNode info, GridId forcedGridId)
        {
            // sane defaults
            ushort csz = 16;
            ushort tsz = 1;

            foreach (var kvInfo in info)
            {
                var key = kvInfo.Key.ToString();
                var val = kvInfo.Value.ToString();
                if (key == "chunksize")
                    csz = ushort.Parse(val);
                else if (key == "tilesize")
                    tsz = ushort.Parse(val);
                else if (key == "snapsize")
                    continue; // obsolete
            }

            var grid = mapMan.CreateUnboundGrid(forcedGridId, csz);
            grid.TileSize = tsz;
            return grid;
        }

        public static void DeserializeChunks(IMapManagerInternal mapMan, IMapGridInternal grid, YamlSequenceNode chunks,
            IReadOnlyDictionary<ushort, string> tileDefMapping,
            ITileDefinitionManager tileDefinitionManager)
        {
            foreach (var chunkNode in chunks.Cast<YamlMappingNode>())
            {
                DeserializeChunk(mapMan, grid, chunkNode, tileDefMapping, tileDefinitionManager);
            }
        }

        private static void DeserializeChunk(IMapManager mapMan, IMapGridInternal grid,
            YamlMappingNode chunkData,
            IReadOnlyDictionary<ushort, string> tileDefMapping,
            ITileDefinitionManager tileDefinitionManager)
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
    }
}
