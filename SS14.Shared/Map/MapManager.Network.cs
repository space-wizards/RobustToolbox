using System;
using System.Diagnostics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network.Messages;

namespace SS14.Shared.Map
{
    public partial class MapManager
    {
        [Dependency] //TODO: kill self
        private readonly IMapManager _mapManager;

        [Dependency]
        private readonly INetManager _netManager;

        [Dependency]
        private readonly ITileDefinitionManager _defManager;


        /// <summary>
        ///     The accepted version of the NetworkMessage map format.
        /// </summary>
        private const int MapVersion = 1;

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public void NetSetup()
        {
            _netManager.RegisterNetMessage<MsgMap>(MsgMap.NAME, (int)MsgMap.ID, message => HandleNetworkMessage((MsgMap)message));

            if(_netManager.IsServer)
                _mapManager.OnTileChanged += MapMgrOnTileChanged;
        }

        /// <summary>
        ///     Default finalizer.
        /// </summary>
        ~MapManager()
        {
            if (_netManager.IsServer)
                _mapManager.OnTileChanged -= MapMgrOnTileChanged;
        }

        /// <inheritdoc />
        public void HandleNetworkMessage(MsgMap message)
        {
            switch (message.MessageType)
            {
                case MapMessage.TurfClick:
                    HandleTurfClick(message);
                    break;
                case MapMessage.TurfUpdate:
                    HandleTileUpdate(message);
                    break;
                case MapMessage.SendTileMap:
                    HandleTileMap(message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message));
            }
        }

        /// <inheritdoc />
        public void SendMap(INetChannel channel)
        {
            Debug.Assert(_netManager.IsServer, "Why is the client calling this?");

            Logger.Log(channel.RemoteAddress + ": Sending map");

            foreach(Map Map in GetAllMaps())
            {
                foreach(IMapGrid Grid in Map.GetAllGrids())
                {
                    var message = _netManager.CreateNetMessage<MsgMap>();
                    message.MessageType = MapMessage.SendTileMap;
                    message.MapIndex = Map.Index;
                    message.GridIndex = Grid.Index;
                    // Tile definition mapping
                    message.TileDefs = new MsgMap.TileDef[_defManager.Count];

                    for (var i = 0; i < _defManager.Count; i++)
                    {
                        message.TileDefs[i] = new MsgMap.TileDef()
                        {
                            Name = _defManager[i].Name
                        };
                    }

                    // Map chunks
                    var grid = _mapManager.GetMap(Map.Index).GetGrid(Grid.Index);
                    var gridSize = grid.ChunkSize;
                    message.ChunkSize = gridSize;
                    message.ChunkDefs = new MsgMap.ChunkDef[grid.ChunkCount];
                    var defCounter = 0;
                    foreach (var chunk in grid.GetMapChunks())
                    {
                        var newChunk = new MsgMap.ChunkDef()
                        {
                            X = chunk.X,
                            Y = chunk.Y
                        };

                        newChunk.Tiles = new uint[gridSize * gridSize];
                        var counter = 0;
                        foreach (var tile in chunk)
                        {
                            newChunk.Tiles[counter] = (uint)tile.Tile;
                            counter++;
                        }

                        message.ChunkDefs[defCounter++] = newChunk;
                    }

                    _netManager.ServerSendMessage(message, channel);
                }
            }
        }

        /// <summary>
        ///     Event handler for when a tile is modified in the MapManager.
        /// </summary>
        /// <param name="gridId">The id of the grid being modified.</param>
        /// <param name="tileRef">A reference to the new tile.</param>
        /// <param name="oldTile">The old tile being modified.</param>
        private void MapMgrOnTileChanged(int gridId, TileRef tileRef, Tile oldTile)
        {
            Debug.Assert(_netManager.IsServer, "Why is the client calling this?");

            var message = _netManager.CreateNetMessage<MsgMap>();

            message.MessageType = MapMessage.TurfUpdate;
            message.SingleTurf = new MsgMap.Turf
            {
                X = tileRef.X,
                Y = tileRef.Y,
                Tile = (uint) tileRef.Tile
            };

            _netManager.ServerSendToAll(message);
        }

        private void HandleTurfClick(MsgMap message)
        {
            /*

            // Who clicked and on what tile.
            Atom.Atom clicker = SS13Server.Singleton.playerManager.GetSessionByConnection(message.SenderConnection).attachedAtom;
            short x = message.ReadInt16();
            short y = message.ReadInt16();

            if (Vector2.Distance(clicker.position, new Vector2(x * tileSpacing + (tileSpacing / 2), y * tileSpacing + (tileSpacing / 2))) > 96)
            {
                return; // They were too far away to click us!
            }
            bool Update = false;
            if (IsSaneArrayPosition(x, y))
            {
                Update = tileArray[x, y].ClickedBy(clicker);
                if (Update)
                {
                    if (tileArray[x, y].tileState == TileState.Dead)
                    {
                        Tiles.Atmos.GasCell g = tileArray[x, y].gasCell;
                        Tiles.Tile t = GenerateNewTile(x, y, tileArray[x, y].tileType);
                        tileArray[x, y] = t;
                        tileArray[x, y].gasCell = g;
                    }
                    NetworkUpdateTile(x, y);
                }
            }

            */
        }

        /// <summary>
        ///     Deserializes an IMapManager and ITileDefinitionManager from a properly formatted NetMessage.
        /// </summary>
        /// <param name="message">The message containing a serialized map and tileDefines.</param>
        private void HandleTileMap(MsgMap message)
        {
            Debug.Assert(_netManager.IsClient, "Why is the server calling this?");

            var mapIndex = message.MapIndex;
            var gridIndex = message.GridIndex;

            _defManager.RegisterServerTileMapping(message);

            var chunkSize = message.ChunkSize;
            var chunkCount = message.ChunkDefs.Length;

            if (!_mapManager.TryGetMap(mapIndex, out IMap map))
                map = _mapManager.CreateMap(mapIndex);
            if (!map.TryGetGrid(gridIndex, out IMapGrid grid))
                grid = _mapManager.GetMap(mapIndex).CreateGrid(gridIndex, chunkSize);

            for (var i = 0; i < chunkCount; ++i)
            {
                var chunkPos = new MapGrid.Indices(message.ChunkDefs[i].X, message.ChunkDefs[i].Y);
                var chunk = grid.GetChunk(chunkPos);

                var counter = 0;
                for (ushort x = 0; x < chunk.ChunkSize; x++)
                {
                    for (ushort y = 0; y < chunk.ChunkSize; y++)
                    {
                        chunk.SetTile(x, y, (Tile)message.ChunkDefs[i].Tiles[counter]);
                        counter++;
                    }
                }
            }
        }

        /// <summary>
        ///     Updates a single tile from the network message.
        /// </summary>
        /// <param name="message">The message containing the info.</param>
        private void HandleTileUpdate(MsgMap message)
        {
            Debug.Assert(_netManager.IsClient, "Why is the server calling this?");

            var x = message.SingleTurf.X;
            var y = message.SingleTurf.Y;
            var tile = (Tile) message.SingleTurf.Tile;
            
            LocalCoordinates coords = new LocalCoordinates(x, y, message.GridIndex, message.MapIndex);
            coords.Grid.SetTile(coords, tile); //TODO: Fix this
        }
    }
}
