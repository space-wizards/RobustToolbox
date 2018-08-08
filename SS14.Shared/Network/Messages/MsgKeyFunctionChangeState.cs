using System;
using Lidgren.Network;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgKeyFunctionStateChange : NetMessage
    {
        #region REQUIRED
        public const MsgGroups GROUP = MsgGroups.Command;
        public static readonly string NAME = nameof(MsgKeyFunctionStateChange);
        public MsgKeyFunctionStateChange(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public KeyFunctionId KeyFunction { get; set; }
        public BoundKeyState NewState { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            KeyFunction = new KeyFunctionId(buffer.ReadInt32());
            NewState = (BoundKeyState)buffer.ReadByte();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((int)KeyFunction);
            buffer.Write((int)NewState);
        }
    }
}
