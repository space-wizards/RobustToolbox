using Robust.Server.Interfaces.Debugging;
using Robust.Shared.Interfaces.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;

namespace Robust.Server.Debugging
{
    internal class DebugDrawingManager : IDebugDrawingManager
    {
#pragma warning disable 649
        [Dependency] private readonly INetManager _net;
#pragma warning restore 649
        public void Initialize()
        {
            _net.RegisterNetMessage<MsgRay>(MsgRay.NAME);
        }
    }
}
