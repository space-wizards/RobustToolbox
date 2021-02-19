using Lidgren.Network;
using Robust.Shared.Utility;

namespace Robust.Shared.Network.Messages
{
    public class MsgReloadPrototypes : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgReloadPrototypes);

        public MsgReloadPrototypes(INetChannel channel) : base(NAME, GROUP)
        {
        }

        #endregion

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
