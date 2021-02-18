using System;
using System.IO;
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
            return new(message.ReadInt32());
        }

        public static void Write(this NetOutgoingMessage message, EntityUid entityUid)
        {
            message.Write((int)entityUid);
        }

        public static GameTick ReadGameTick(this NetIncomingMessage message)
        {
            return new(message.ReadUInt32());
        }

        public static void Write(this NetOutgoingMessage message, GameTick tick)
        {
            message.Write(tick.Value);
        }

        public static Guid ReadGuid(this NetIncomingMessage message)
        {
            Span<byte> span = stackalloc byte[16];
            message.ReadBytes(span);
            return new Guid(span);
        }

        public static void Write(this NetOutgoingMessage message, Guid guid)
        {
            Span<byte> span = stackalloc byte[16];
            guid.TryWriteBytes(span);
            message.Write(span);
        }

        public static Color ReadColor(this NetIncomingMessage message)
        {
            var rByte = message.ReadByte();
            var gByte = message.ReadByte();
            var bByte = message.ReadByte();
            var aByte = message.ReadByte();
            return new Color(rByte, gByte, bByte, aByte);
        }

        public static void Write(this NetOutgoingMessage message, Color color)
        {
            message.Write(color.RByte);
            message.Write(color.GByte);
            message.Write(color.BByte);
            message.Write(color.AByte);
        }

        /// <summary>
        ///     Reads byte-aligned data as a memory stream.
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     Thrown if the current read position of the message is not byte-aligned.
        /// </exception>
        public static MemoryStream ReadAlignedMemory(this NetIncomingMessage message, int length)
        {
            if ((message.Position & 7) != 0)
            {
                throw new ArgumentException("Read position in message must be byte-aligned", nameof(message));
            }

            var stream = new MemoryStream(message.Data, message.PositionInBytes, length, false);
            message.Position += length * 8;
            return stream;
        }
    }
}
