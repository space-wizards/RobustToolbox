using Robust.Client.Interfaces.Debugging;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robust.Client.Debugging
{
    internal class DebugDrawingManager : IDebugDrawingManager
    {
#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _net;
#pragma warning restore 649
        public void Initialize()
        {
            _net.RegisterNetMessage<MsgRay>(MsgRay.NAME, HandleDrawRay);
        }

        public void Update(float frameTime)
        {
            throw new NotImplementedException();
        }

        private void HandleDrawRay(MsgRay msg)
        {

        }
    }
}
