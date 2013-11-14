using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace ServerInterfaces.ClientConsoleHost
{
    public interface IClientConsoleHost
    {
        void ProcessCommand(string text, NetConnection sender);
        void SendConsoleReply(string text, NetConnection target);
    }
}
