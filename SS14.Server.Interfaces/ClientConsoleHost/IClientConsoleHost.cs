using Lidgren.Network;

namespace SS14.Server.Interfaces.ClientConsoleHost
{
    public interface IClientConsoleHost
    {
        void ProcessCommand(string text, NetConnection sender);
        void SendConsoleReply(string text, NetConnection target);
        void HandleRegistrationRequest(NetConnection senderConnection);
    }
}
