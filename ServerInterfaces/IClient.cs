using Lidgren.Network;
using SS13_Shared;

namespace ServerInterfaces
{
    public interface IClient
    {
        NetConnection NetConnection { get; }
        string PlayerName { get; set; }
        ClientStatus Status { get; set; }
        ushort MobID { get; set; }
    }
}