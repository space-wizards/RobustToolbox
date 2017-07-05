using System.Collections.Generic;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Network;

namespace SS14.Server.Interfaces.ClientConsoleHost
{
    public interface IClientConsoleHost
    {
        IDictionary<string, IClientCommand> AvailableCommands { get; }
        void ProcessCommand(string text, INetChannel sender);
        void SendConsoleReply(string text, INetChannel target);
        void HandleRegistrationRequest(INetChannel senderConnection);
    }
}
