using Lidgren.Network;
using Robust.Shared.Utility;

namespace Robust.Shared.Network.Messages
{
    [NetMessage(MsgGroups.Command)]
    public class MsgReloadPrototypes : NetMessage
    {
        public ResourcePath[] Paths = default!;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var count = buffer.ReadInt32();
            Paths = new ResourcePath[count];

            for (var i = 0; i < count; i++)
            {
                Paths[i] = new ResourcePath(buffer.ReadString());
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(Paths.Length);

            foreach (var path in Paths)
            {
                buffer.Write(path.ToString());
            }
        }
    }
}
