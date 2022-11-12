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

            var mapSystem = dependencies.Resolve<IEntitySystemManager>().GetEntitySystem<SharedMapSystem>();
            mapSystem.SuppressOnTileChanged = true;

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
            mapSystem.SuppressOnTileChanged = false;

            return chunk;
        }

        public DataNode Write(ISerializationManager serializationManager, MapChunk value, IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var root = new MappingDataNode();
            var ind = new ValueDataNode($"{value.X},{value.Y}");
            root.Add("ind", ind);

            var gridNode = new ValueDataNode();
            root.Add("tiles", gridNode);

            gridNode.Value = MapGridComponent.SerializeTiles(value);

            return root;
        }

        public MapChunk Copy(ISerializationManager serializationManager, MapChunk source, MapChunk target, bool skipHook,
            ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }
    }
}
