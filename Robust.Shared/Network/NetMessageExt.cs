using System;
using System.IO;
using System.Numerics;
using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Network
{
    public static class NetMessageExt
    {
        public static NetCoordinates ReadNetCoordinates(this NetIncomingMessage message)
        {
            var entity = message.ReadNetEntity();
            var vector = message.ReadVector2();

            return new NetCoordinates(entity, vector);
        }

        public static void Write(this NetOutgoingMessage message, NetCoordinates coordinates)
        {
            message.Write(coordinates.NetEntity);
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

        public static NetEntity ReadNetEntity(this NetIncomingMessage message)
        {
            return new(message.ReadInt32());
        }

        public static void Write(this NetOutgoingMessage message, NetEntity entity)
        {
            message.Write((int)entity);
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
            message.AssertAtLeastSize(SizeOfGuid);
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
        public static void ReadAlignedMemory(this NetIncomingMessage message, MemoryStream memStream, int length)
        {
            if ((message.Position & 7) != 0)
            {
                throw new ArgumentException("Read position in message must be byte-aligned", nameof(message));
            }

            message.AssertAtLeastSize(length);
            DebugTools.Assert(memStream.Position == 0);
            memStream.Write(message.Data, message.PositionInBytes, length);
            memStream.Position = 0;
            message.Position += length * 8;
        }

        public static TimeSpan ReadTimeSpan(this NetIncomingMessage message)
        {
            return TimeSpan.FromTicks(message.ReadInt64());
        }

        public static void Write(this NetOutgoingMessage message, TimeSpan timeSpan)
        {
            message.Write(timeSpan.Ticks);
        }

        /// <summary>
        /// Asserts that the buffer has at least the remaining size to read size bytes.
        /// </summary>
        /// <param name="message">Incoming message/buffer</param>
        /// <param name="size">Number of bytes that need to be available to read</param>
        /// <exception cref="InvalidOperationException">Thrown if there is not enough size in the message to read the size, or it's a negative size</exception>
        public static void AssertAtLeastSize(this NetIncomingMessage message, long size)
        {
            var remainingBits = message.LengthBits - message.Position;
            if (size < 0 || size > remainingBits / 8)
            {
                throw new InvalidOperationException(
                    $"Unable to read {size} bytes with only {remainingBits} bits left in the message.");
            }
        }

        /// <summary>
        /// Lidgren zig-zag encoding.
        /// </summary>
        public const uint MinSizeOfVariableInt = 1;

        /// <summary>
        /// Empty strings are one byte.
        /// </summary>
        public const uint MinSizeOfString = 1;

        /// <summary>
        /// Guids are 16 bytes.
        /// </summary>
        public const uint SizeOfGuid = 16;

        /// <summary>
        /// TimeSpans are int64s.
        /// </summary>
        [PublicAPI]
        public const uint SizeOfTimeSpan = sizeof(long);

        /// <summary>
        /// ResPaths are strings.
        /// </summary>
        public const uint MinSizeOfResPath = MinSizeOfString;

        /// <summary>
        /// NetEntities are int32s
        /// </summary>
        public const uint SizeOfNetEntity = sizeof(int);

        /// <summary>
        /// Encryption payload is 1024 at the max (arbitrary number).
        /// </summary>
        public const uint MaxSizeOfEncryption = 1024;

        /// <summary>
        /// HWIDs are 1024 at the max (arbitrary number).
        /// </summary>
        public const uint MaxSizeOfHwid = 1024;
    }
}
