using System;
using System.IO;
using Lidgren.Network;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgState : NetMessage
    {
        // If a state is large enough we send it ReliableUnordered instead.
        // This is to avoid states being so large that they consistently fail to reach the other end
        // (due to being in many parts).
        public const int ReliableThreshold = 1300;

        public override MsgGroups MsgGroup => MsgGroups.Entity;

        public GameState State;

        private bool _hasWritten;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            MsgSize = buffer.LengthBytes;
            var length = buffer.ReadVariableInt32();
            using var stream = buffer.ReadAlignedMemory(length);
            var serializer = IoCManager.Resolve<IRobustSerializer>();
            serializer.DeserializeDirect(stream, out State);

            State.PayloadSize = length;
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            var serializer = IoCManager.Resolve<IRobustSerializer>();
            using (var stateStream = new MemoryStream())
            {
                DebugTools.Assert(stateStream.Length <= Int32.MaxValue);
                serializer.SerializeDirect(stateStream, State);
                buffer.WriteVariableInt32((int) stateStream.Length);

                // Always succeeds.
                stateStream.TryGetBuffer(out var segment);
                buffer.Write(segment);
            }

            _hasWritten = false;
            MsgSize = buffer.LengthBytes;
        }

        /// <summary>
        ///     Whether this state message is large enough to warrant being sent reliably.
        ///     This is only valid after
        /// </summary>
        /// <returns></returns>
        public bool ShouldSendReliably()
        {
            // This check will be true in integration tests.
            // TODO: Maybe handle this better so that packet loss integration testing can be done?
            if (!_hasWritten)
            {
                return true;
            }
            return MsgSize > ReliableThreshold;
        }

        public override NetDeliveryMethod DeliveryMethod
        {
            get
            {
                if (ShouldSendReliably())
                {
                    return NetDeliveryMethod.ReliableUnordered;
                }

                return base.DeliveryMethod;
            }
        }
    }
}
