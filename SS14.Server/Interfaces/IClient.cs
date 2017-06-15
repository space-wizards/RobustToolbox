using Lidgren.Network;
using SS14.Shared;

namespace SS14.Server.Interfaces
{
    public interface IClient
    {
        NetConnection NetConnection { get; }
        string PlayerName { get; set; }
        ClientStatus Status { get; set; }
        ushort MobID { get; set; }
    }
}