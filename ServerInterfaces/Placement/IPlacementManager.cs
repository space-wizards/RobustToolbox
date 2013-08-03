using GameObject;
using Lidgren.Network;

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
        void SendPlacementBegin(Entity mob, int range, string objectType, string alignOption);

        /// <summary>
        ///  Places mob in tile placement mode with given settings.
        /// </summary>
        void SendPlacementBeginTile(Entity mob, int range, string tileType, string alignOption);

        /// <summary>
        ///  Cancels object placement mode for given mob.
        /// </summary>
        void SendPlacementCancel(Entity mob);

        /// <summary>
        ///  Gives Mob permission to place entity and places it in object placement mode.
        /// </summary>
        void StartBuilding(Entity mob, int range, string objectType, string alignOption);

        /// <summary>
        ///  Gives Mob permission to place tile and places it in object placement mode.
        /// </summary>
        void StartBuildingTile(Entity mob, int range, string tileType, string alignOption);

        /// <summary>
        ///  Revokes open placement Permission and cancels object placement mode.
        /// </summary>
        void CancelBuilding(Entity mob);

        /// <summary>
        ///  Gives a mob a permission to place a given Entity.
        /// </summary>
        void AssignBuildPermission(Entity mob, int range, string objectType, string alignOption);

        /// <summary>
        ///  Gives a mob a permission to place a given Tile.
        /// </summary>
        void AssignBuildPermissionTile(Entity mob, int range, string tileType, string alignOption);

        /// <summary>
        ///  Removes all building Permissions for given mob.
        /// </summary>
        void RevokeAllBuildPermissions(Entity mob);
    }
}