using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Interfaces
{
    interface IBaseClient
    {
        ushort DefaultPort { get; }

        ClientRunLevel RunLevel { get; }

        event EventHandler<RunLevelChangedEvent> RunLevelChanged;

        void Initialize();

        void ConnectToServer(string ip, ushort port);

        void DisconnectFromServer(string reason);
    }
}
