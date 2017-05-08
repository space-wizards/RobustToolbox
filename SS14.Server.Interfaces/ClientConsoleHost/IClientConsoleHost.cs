using Lidgren.Network;
using System.Collections.Generic;

namespace SS14.Server.Interfaces.ClientConsoleHost
{
    public interface IClientConsoleHost
    {
        IDictionary<string, IClientCommand> AvailableCommands { get; }
        void ProcessCommand(string text, NetConnection sender);
        void SendConsoleReply(string text, NetConnection target);
        void HandleRegistrationRequest(NetConnection senderConnection);
    }
}
