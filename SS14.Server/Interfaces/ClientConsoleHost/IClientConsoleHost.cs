using System.Collections.Generic;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network.Messages;

namespace SS14.Server.Interfaces.ClientConsoleHost
{
    public interface IClientConsoleHost
    {
        IDictionary<string, IClientCommand> AvailableCommands { get; }

        void Initialize();
        void ProcessCommand(MsgConCmd message);
        void SendConsoleReply(INetChannel target, string text);
        void HandleRegistrationRequest(INetChannel senderConnection);
    }
}
