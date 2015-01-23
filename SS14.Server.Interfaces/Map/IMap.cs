using System;
using System.Drawing;
using Lidgren.Network;
using SS13_Shared;
using ServerInterfaces.Tiles;

namespace ServerInterfaces.Map
{
    public interface IMap
    {
        bool InitMap(string mapName);
        void HandleNetworkMessage(NetIncomingMessage message);
        void NetworkUpdateTile(int x, int y);
        void SaveMap();
        TileType[,] GetMapForSending();
        ITile GetTileAt(int x, int y);
        void AddGasAt(Point position, GasType type, int amount);
        float GetGasAmount(Point position, GasType type);
        void UpdateAtmos();
        void SendAtmosStateTo(NetConnection client);

        /// <summary>
        /// This function takes the gas cell from one tile and moves it to another, reconnecting all of the references in adjacent tiles.
        /// Use this when a new tile is generated at a map location.
        /// </summary>
        /// <param name="fromTile">Tile to move gas information/cell from</param>
        /// <param name="toTile">Tile to move gas information/cell to</param>
        void MoveGasCell(ITile fromTile, ITile toTile);

        bool ChangeTile(int x, int z, TileType newType);
        bool ChangeTile(int x, int z, Type newType);
        ITile GenerateNewTile(int x, int y, TileType type);
        NetOutgoingMessage CreateMapMessage(MapMessage messageType);

        /// <summary>
        /// Lol fuck
        /// </summary>
        /// <param name="message"></param>
        void SendMessage(NetOutgoingMessage message);

        void Shutdown();
        int GetMapWidth();
        int GetMapHeight();
        Point GetTileArrayPositionFromWorldPosition(double x, double z);
        ITile GetTileFromWorldPosition(Vector2 pos);
        Point GetTileArrayPositionFromWorldPosition(Vector2 pos);
        TileType GetObjectTypeFromWorldPosition(float x, float z);
        bool IsSaneArrayPosition(int x, int y);
        bool CheckCollision(Vector2 pos);
        TileType GetObjectTypeAt(Vector2 pos);
        bool IsFloorUnder(Vector2 pos);
    }
}