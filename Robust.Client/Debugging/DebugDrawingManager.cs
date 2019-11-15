using Robust.Client.Interfaces.Debugging;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Client.Graphics.Clyde;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Drawing;
using Robust.Shared.Maths;

namespace Robust.Client.Debugging
{
    internal class DebugDrawingManager : Overlay, IDebugDrawingManager
    {
#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _net;
#pragma warning restore 649

        private List<Ray> rays;
        public void Initialize()
        {
            _net.RegisterNetMessage<MsgRay>(MsgRay.NAME, HandleDrawRay);
            rays = new List<Ray>();
        }
        public DebugDrawingManager() : base(nameof(DebugDrawingManager))
        {

        }
        public void Update(float frameTime)
        {
            //throw new NotImplementedException();
        }

        protected override void Draw(DrawingHandleBase handle)
        {   foreach(Ray ray in rays)
            {
                handle.DrawLine(ray.Position, ray.Direction * 2f, Color.Green);
            }

        }

        private void HandleDrawRay(MsgRay msg)
        {
            var newRay = msg.RayToSend;
            rays.Add(newRay);
        }
    }
}
