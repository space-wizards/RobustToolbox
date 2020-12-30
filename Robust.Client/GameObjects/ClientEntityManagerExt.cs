using Robust.Client.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Robust.Client.GameObjects
{
    public static class ClientEntityManagerExt
    {
        /// <summary>
        ///     Converts the client-side entity UID of an <see cref="EntityCoordinates"/> to the server-side UID.
        /// </summary>
        public static EntityCoordinates ToServerCoordinates(
            this IClientEntityManager entityMgr,
            in EntityCoordinates clientCoords)
        {
            return new(entityMgr.GetServerId(clientCoords.EntityId), clientCoords.Position);
        }

        /// <summary>
        ///     Converts the server-side entity UID of an <see cref="EntityCoordinates"/> to the client-side UID.
        /// </summary>
        public static EntityCoordinates ToClientCoordinates(
            this IClientEntityManager entityMgr,
            in EntityCoordinates serverCoords)
        {
            return new(entityMgr.GetClientId(serverCoords.EntityId), serverCoords.Position);
        }
    }
}
