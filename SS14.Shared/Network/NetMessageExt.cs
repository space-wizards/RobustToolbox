using Lidgren.Network;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Shared.Network
{
    public static class NetMessageExt
    {
        public static GridCoordinates ReadGridLocalCoordinates(this NetIncomingMessage message)
        {
            var gridId = new GridId(message.ReadInt32());
            var vector = message.ReadVector2();

            return new GridCoordinates(vector, gridId);
        }

        public static void Write(this NetOutgoingMessage message, GridCoordinates coordinates)
        {
            message.Write(coordinates.GridID.Value);
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
    }
}
