using System;

namespace Robust.Server.ServerStatus
{
    public interface IWatchdogApi
    {
        event Action UpdateReceived;

        void Heartbeat();

        void Initialize();
    }
}
