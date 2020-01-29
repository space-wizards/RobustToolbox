using System;
using System.Net;

namespace Robust.Shared.Interfaces.Network
{
    /// <summary>
    /// The server version of the INetManager.
    /// </summary>
    public interface IServerNetManager : INetManager
    {
        event Func<IPEndPoint, string> JudgeConnection;
    }
}
