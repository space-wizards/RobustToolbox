using System;
using System.Collections.Generic;
using System.Diagnostics;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network.Messages;

namespace SS14.Shared.Map
{
    public partial class MapManager
    {
        [Dependency]
        private readonly INetManager _netManager;

        [Dependency]
        private readonly ITileDefinitionManager _defManager;

        private int _gridsToReceive = -1;
        private int _gridsReceived;

        public void SendMap(INetChannel channel)
        {
            Debug.Assert(_netManager.IsServer, "Why is the client calling this?");

            Logger.Log(channel.RemoteAddress + ": Sending map");

            var quantityGridsSent = 0;

            foreach (var map in GetAllMaps())
            {
                foreach (var grid in map.GetAllGrids())
                {
                    quantityGridsSent++;
                    var message = _netManager.CreateNetMessage<MsgMap>();
                    message.MessageType = MapMessage.SendTileMap;
                    message.MapIndex = map.Index;
                    message.GridIndex = grid.Index;
                    // Tile definition mapping
                    message.TileDefs = new MsgMap.TileDef[_defManager.Count];

                    for (var i = 0; i < _defManager.Count; i++)
                    {
                        message.TileDefs[i] = new MsgMap.TileDef
                        {
                            Name = _defManager[i].Name
                        };
                    }

                    // Map chunks
                    var gridSize = grid.ChunkSize;
                    message.ChunkSize = gridSize;
                    message.ChunkDefs = new MsgMap.ChunkDef[grid.ChunkCount];
                    var defCounter = 0;
                    foreach (var chunk in grid.GetMapChunks())
                    {
                        var newChunk = new MsgMap.ChunkDef
                        {
                            X = chunk.X,
                            Y = chunk.Y
                        };

                        newChunk.Tiles = new uint[gridSize * gridSize];
                        var counter = 0;
                        foreach (var tile in chunk)
                        {
                            newChunk.Tiles[counter] = (uint) tile.Tile;
                            counter++;
                        }

                        message.ChunkDefs[defCounter++] = newChunk;
                    }

                    _netManager.ServerSendMessage(message, channel);
                }
            }
            var msg = _netManager.CreateNetMessage<MsgMap>();
            msg.MessageType = MapMessage.SendMapInfo;
            msg.MapGridsToSend = quantityGridsSent;
            _netManager.ServerSendMessage(msg, channel);
        }

        private void HandleNetworkMessage(MsgMap message)
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
                case MapMessage.SendMapInfo:
                    CollectMapInfo(message);
                    break;
                case MapMessage.CreateMap:
                    CreateMap(message.MapIndex);
                    break;
                case MapMessage.DeleteMap:
                    DeleteMap(message.MapIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message));
            }
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

            _gridsReceived++;

            var mapIndex = message.MapIndex;
            var gridIndex = message.GridIndex;

            _defManager.RegisterServerTileMapping(message);

            var chunkSize = message.ChunkSize;
            var chunkCount = message.ChunkDefs.Length;

            if (!TryGetMap(mapIndex, out var map))
                map = CreateMap(mapIndex);

            if (!map.GridExists(gridIndex))
                GetMap(mapIndex).CreateGrid(gridIndex, chunkSize);

            var grid = GetMap(mapIndex).GetGrid(gridIndex);

            SuppressOnTileChanged = true;
            var modified = new List<(int x, int y, Tile tile)>();

            for (var i = 0; i < chunkCount; ++i)
            {
                var chunkPos = new MapGrid.Indices(message.ChunkDefs[i].X, message.ChunkDefs[i].Y);
                var chunk = grid.GetChunk(chunkPos);

                var counter = 0;
                for (ushort x = 0; x < chunk.ChunkSize; x++)
                {
                    for (ushort y = 0; y < chunk.ChunkSize; y++)
                    {
                        var tile = (Tile) message.ChunkDefs[i].Tiles[counter];
                        if (chunk.GetTile(x, y).Tile != tile)
                        {
                            chunk.SetTile(x, y, tile);
                            modified.Add((x + chunk.X * chunk.ChunkSize, y + chunk.Y * chunk.ChunkSize, tile));
                        }
                        counter++;
                    }
                }
            }

            SuppressOnTileChanged = false;
            if (modified.Count != 0)
            {
                GridChanged?.Invoke(this, new GridChangedEventArgs(grid, modified));
            }

            if (_gridsReceived == _gridsToReceive)
                IoCManager.Resolve<IEntityManager>().MapsInitialized = true;
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

            var pos = new LocalCoordinates(x, y, message.GridIndex, message.MapIndex);
            pos.Grid.SetTile(pos, tile);
        }

        private void CollectMapInfo(MsgMap message)
        {
            Debug.Assert(_netManager.IsClient, "Why is the server calling this?");

            _gridsToReceive = message.MapGridsToSend;

            if (_gridsReceived == _gridsToReceive)
                IoCManager.Resolve<IEntityManager>().MapsInitialized = true;
        }
    }
}
