using System;
using System.Globalization;
using System.IO;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.Maps
{
    public static class YamlGridSerializer
    {
        public static YamlMappingNode SerializeGrid(IMapGrid grid)
        {
            var gridn = new YamlMappingNode();
            var info = new YamlMappingNode();
            var chunkSeq = new YamlSequenceNode();

            gridn.Add("settings", info);
            gridn.Add("chunks", chunkSeq);

            info.Add("csz", grid.ChunkSize.ToString(CultureInfo.InvariantCulture));
            info.Add("tsz", grid.TileSize.ToString(CultureInfo.InvariantCulture));
            info.Add("sgsz", grid.SnapSize.ToString(CultureInfo.InvariantCulture));

            var chunks = grid.GetMapChunks();
            foreach (var chunk in chunks)
            {
                var chunkNode = SerializeChunk(chunk);
                chunkSeq.Add(chunkNode);
            }

            var root = new YamlMappingNode();
            root.Add("grid", gridn);
            return root;
        }

        private static YamlNode SerializeChunk(IMapChunk chunk)
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

        private static string SerializeTiles(IMapChunk chunk)
        {
            // number of bytes written per tile, because sizeof(Tile) is useless.
            const int structSize = 4;

            var nTiles = chunk.ChunkSize * chunk.ChunkSize * structSize;
            byte[] barr = new byte[nTiles];

            using (var stream = new MemoryStream(barr))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (var tileRef in chunk)
                    {
                        // if you change these types, fix structSize!!!
                        writer.Write(tileRef.Tile.TileId);
                        writer.Write(tileRef.Tile.Data);
                    }
                }
            }

            return Convert.ToBase64String(barr);
        }

        public static void DeserializeGrid(IMapManager mapMan, IMap map, GridId gridId, YamlMappingNode info, YamlSequenceNode chunks)
        {
            ushort csz = 0;
            ushort tsz = 0;
            float sgsz = 0.0f;

            foreach (var kvInfo in info)
            {
                var key = kvInfo.Key.ToString();
                var val = kvInfo.Value.ToString();
                if (key == "csz")
                    csz = ushort.Parse(val);
                else if (key == "tsz")
                    tsz = ushort.Parse(val);
                else if (key == "sgsz")
                    sgsz = float.Parse(val);
            }

            var grid = map.CreateGrid(gridId, csz, sgsz);

            foreach (YamlMappingNode chunkNode in chunks)
            {
                DeserializeChunk(mapMan, grid, chunkNode);
            }
        }

        private static void DeserializeChunk(IMapManager mapMan, IMapGrid grid, YamlMappingNode chunk)
        {
            var indNode = chunk["ind"];
            var tileNode = chunk["tiles"];

            var indices = indNode.AsVector2i() * grid.ChunkSize;
            var tileBytes = Convert.FromBase64String(tileNode.ToString());

            using (var stream = new MemoryStream(tileBytes))
            {
                using (var reader = new BinaryReader(stream))
                {
                    mapMan.SuppressOnTileChanged = true;
                    
                    for(var x = 0; x < grid.ChunkSize; x++)
                    for (var y = 0; y < grid.ChunkSize; y++)
                    {
                        var id = reader.ReadUInt16();
                        var data = reader.ReadUInt16();

                        var tile = new Tile(id, data);
                        grid.SetTile(new LocalCoordinates(x + indices.X, y + indices.Y, grid.Index, grid.MapID), tile);
                    }

                    mapMan.SuppressOnTileChanged = false;
                }
            }
        }
    }
}
