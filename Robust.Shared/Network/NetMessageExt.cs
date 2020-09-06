using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Shared.Network
{
    public static class NetMessageExt
    {
        public static EntityCoordinates ReadEntityCoordinates(this NetIncomingMessage message)
        {
            var entityUid = new EntityUid(message.ReadInt32());
            var vector = message.ReadVector2();

            return new EntityCoordinates(entityUid, vector);
        }

        public static void Write(this NetOutgoingMessage message, EntityCoordinates coordinates)
        {
            message.Write(coordinates.EntityId);
            message.Write(coordinates.Position);
        }

        public static Vector2 ReadVector2(this NetIncomingMessage message)
        {
            var x = message.ReadFloat();
            var y = message.ReadFloat();

            return new Vector2(x, y);
        }

        public static void Write(this NetOutgoingMessage message, Vector2 vector2)
        {
            message.Write(vector2.X);
            message.Write(vector2.Y);
        }

        public static EntityUid ReadEntityUid(this NetIncomingMessage message)
        {
            return new EntityUid(message.ReadInt32());
        }

        public static void Write(this NetOutgoingMessage message, EntityUid entityUid)
        {
            message.Write((int)entityUid);
        }

        public static GameTick ReadGameTick(this NetIncomingMessage message)
        {
            return new GameTick(message.ReadUInt32());
        }

        public static void Write(this NetOutgoingMessage message, GameTick tick)
        {
            message.Write(tick.Value);
        }
    }
}
