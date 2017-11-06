using System;

namespace SS14.Client.Interfaces
{
    public interface IBaseClient : IDisposable
    {
        ushort DefaultPort { get; }

        ClientRunLevel RunLevel { get; }

        event EventHandler<RunLevelChangedEvent> RunLevelChanged;

        void Initialize();

        void Update();

        void Tick();

        void ConnectToServer(string ip, ushort port);

        void DisconnectFromServer(string reason);
    }
}
