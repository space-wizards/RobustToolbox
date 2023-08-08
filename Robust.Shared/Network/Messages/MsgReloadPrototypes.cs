using Lidgren.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgReloadPrototypes : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public ResPath[] Paths = default!;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            var count = buffer.ReadInt32();
            Paths = new ResPath[count];

            for (var i = 0; i < count; i++)
            {
                Paths[i] = new ResPath(buffer.ReadString());
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(Paths.Length);

            foreach (var path in Paths)
            {
                buffer.Write(path.ToString());
            }
        }
    }
}
