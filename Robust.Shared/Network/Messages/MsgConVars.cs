using System;
using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Shared.Network.Messages
{
    internal class MsgConVars : NetMessage
    {
        // Max buffer could potentially be 255 * 128 * 1024 = ~33MB, so if MaxMessageSize starts being a problem it can be increased.
        private const int MaxMessageSize = 0x4000; // Arbitrarily chosen as a 'sane' value as the maximum size of the entire message.
        private const int MaxNameSize = 4 * 32; // UTF8 Max char size is 4 bytes, 32 chars.
        private const int MaxStringValSize = 4 * 256; // UTF8 Max char size is 4 bytes, 256 chars.

        public override MsgGroups MsgGroup => MsgGroups.String;

        public GameTick Tick;
        public List<(string name, object value)> NetworkedVars = null!;

        /// <inheritdoc />
        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            if(buffer.LengthBytes > MaxMessageSize)
                Logger.WarningS("net", $"{MsgChannel}: received a large {nameof(MsgConVars)}, {buffer.LengthBytes}B > {MaxMessageSize}B");

            Tick = new GameTick(buffer.ReadVariableUInt32());
            var nVars = buffer.ReadByte();

            NetworkedVars = new List<(string name, object value)>(nVars);

            for (int i = 0; i < nVars; i++)
            {
                // give the string a smaller bounds than int.MaxValue
                var nameSize = buffer.PeekStringSize();
                if (0 >= nameSize || nameSize > MaxNameSize)
                    throw new InvalidOperationException($"Cvar name size '{nameSize}' is out of bounds (1-{MaxNameSize} bytes).");

                var name = buffer.ReadString();
                var valType = (CvarType)buffer.ReadByte();

                object value;
                switch (valType)
                {
                    case CvarType.Int:
                        value = buffer.ReadInt32();
                        break;
                    case CvarType.Long:
                        value = buffer.ReadInt64();
                        break;
                    case CvarType.Bool:
                        value = buffer.ReadBoolean();
                        break;
                    case CvarType.String:

                        // give the string a smaller bounds than int.MaxValue
                        var strSize = buffer.PeekStringSize();
                        if (0 > strSize || strSize > MaxStringValSize)
                            throw new InvalidOperationException($"Cvar string value size '{nameSize}' for cvar '{name}' is out of bounds (0-{MaxStringValSize} bytes).");

                        value = buffer.ReadString();
                        break;
                    case CvarType.Float:
                        value = buffer.ReadFloat();
                        break;
                    case CvarType.Double:
                        value = buffer.ReadDouble();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                NetworkedVars.Add((name, value));
            }
        }

        /// <inheritdoc />
        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            if(NetworkedVars == null)
                throw new InvalidOperationException($"{nameof(NetworkedVars)} collection is null.");

            if(NetworkedVars.Count > byte.MaxValue)
                throw new InvalidOperationException($"{nameof(NetworkedVars)} collection count is greater than {short.MaxValue}.");

            buffer.WriteVariableUInt32(Tick.Value);
            buffer.Write((byte)NetworkedVars.Count);

            foreach (var (name, value) in NetworkedVars)
            {
                buffer.Write(name);

                switch (value)
                {
                    case int val:
                        buffer.Write((byte)CvarType.Int);
                        buffer.Write(val);
                        break;
                    case long val:
                        buffer.Write((byte)CvarType.Long);
                        buffer.Write(val);
                        break;
                    case bool val:
                        buffer.Write((byte)CvarType.Bool);
                        buffer.Write(val);
                        break;
                    case string val:
                        buffer.Write((byte)CvarType.String);
                        buffer.Write(val);
                        break;
                    case float val:
                        buffer.Write((byte)CvarType.Float);
                        buffer.Write(val);
                        break;
                    case double val:
                        buffer.Write((byte)CvarType.Double);
                        buffer.Write(val);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private enum CvarType : byte
        {
            // ReSharper disable once UnusedMember.Local
            None,

            Int,
            Long,
            Bool,
            String,
            Float,
            Double
        }
    }
}
