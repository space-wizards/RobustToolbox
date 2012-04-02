using Lidgren.Network;
using SS13_Shared;
using ServerInterfaces;
using ServerInterfaces.GameObject;

namespace ServerInterfaces.Placement
{
    public interface IPlacementManager
    {
        void Initialize(ISS13Server server);

        /// <summary>
        ///  Handles placement related client messages.
        /// </summary>
        void HandleNetMessage(NetIncomingMessage msg);

        void HandlePlacementRequest(NetIncomingMessage msg);

        /// <summary>
        ///  Places mob in entity placement mode with given settings.
        /// </summary>
        void SendPlacementBegin(IEntity mob, ushort range, string objectType, PlacementOption alignOption);

        /// <summary>
        ///  Places mob in tile placement mode with given settings.
        /// </summary>
        void SendPlacementBegin(IEntity mob, ushort range, TileType tileType, PlacementOption alignOption);

        /// <summary>
        ///  Cancels object placement mode for given mob.
        /// </summary>
        void SendPlacementCancel(IEntity mob);

        /// <summary>
        ///  Gives Mob permission to place entity and places it in object placement mode.
        /// </summary>
        void StartBuilding(IEntity mob, ushort range, string objectType, PlacementOption alignOption);

        /// <summary>
        ///  Gives Mob permission to place tile and places it in object placement mode.
        /// </summary>
        void StartBuilding(IEntity mob, ushort range, TileType tileType, PlacementOption alignOption);

        /// <summary>
        ///  Revokes open placement Permission and cancels object placement mode.
        /// </summary>
        void CancelBuilding(IEntity mob);

        /// <summary>
        ///  Gives a mob a permission to place a given Entity.
        /// </summary>
        void AssignBuildPermission(IEntity mob, ushort range, string objectType, PlacementOption alignOption);

        /// <summary>
        ///  Gives a mob a permission to place a given Tile.
        /// </summary>
        void AssignBuildPermission(IEntity mob, ushort range, TileType tileType, PlacementOption alignOption);

        /// <summary>
        ///  Removes all building Permissions for given mob.
        /// </summary>
        void RevokeAllBuildPermissions(IEntity mob);
    }
}