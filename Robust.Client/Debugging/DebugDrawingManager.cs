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
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Interfaces.Timing;

namespace Robust.Client.Debugging
{
    internal class DebugDrawingManager : IDebugDrawingManager
    {
#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _net;
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IGameTiming _gameTimer;
#pragma warning restore 649

        private List<RayWithLifetime> raysWithLifeTime;
        private TimeSpan _rayLifeTime;
        private bool _debugDrawRays;

        private struct RayWithLifetime
        {
            public Ray TheRay;
            public TimeSpan LifeTime;
        }
        public bool DebugDrawRays
        {
            get => _debugDrawRays;
            set
            {
                if (value == DebugDrawRays)
                {
                    return;
                }

                _debugDrawRays = value;

                if (value)
                {
                    _overlayManager.AddOverlay(new DebugDrawRayOverlay(raysWithLifeTime));
                }
                else
                {
                    _overlayManager.RemoveOverlay(nameof(DebugDrawRayOverlay));
                }
            }
        }

        public TimeSpan DebugRayLifetime
        {
            get => _rayLifeTime;
            set
            {
               _rayLifeTime = value;

            }
        }

        public void Initialize()
        {
            _net.RegisterNetMessage<MsgRay>(MsgRay.NAME, HandleDrawRay);
            raysWithLifeTime = new List<RayWithLifetime>();
            _rayLifeTime = TimeSpan.FromSeconds(5);
        }

        private void HandleDrawRay(MsgRay msg)
        {   
            var newRay = msg.RayToSend;
            var newRayWithLifetime = new RayWithLifetime
            {
                TheRay = newRay,
                LifeTime = _gameTimer.RealTime + _rayLifeTime
            };
            if(!raysWithLifeTime.Contains(newRayWithLifetime))
            {
                raysWithLifeTime.Add(newRayWithLifetime);
            }
           
           
        }

        public void FrameUpdate(FrameEventArgs frameEventArgs)
        {
            if (!_debugDrawRays)
            {
                return;
            }

            foreach (var rayWL in raysWithLifeTime)
            {
                raysWithLifeTime.RemoveAll(r => r.LifeTime < _rayLifeTime);
            }

        }

        private sealed class DebugDrawRayOverlay : Overlay
        {
            public override OverlaySpace Space => OverlaySpace.WorldSpace;
            private List<RayWithLifetime> raysWithLifeTime;
            public DebugDrawRayOverlay(List<RayWithLifetime> _rays) : base(nameof(DebugDrawRayOverlay))
            {
                raysWithLifeTime = _rays;
            }
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                raysWithLifeTime = new List<RayWithLifetime>();
            }
            protected override void Draw(DrawingHandleBase handle)
            {
                var worldhandle = (DrawingHandleBase)handle;
                foreach(var rayWL in raysWithLifeTime)
                {
                    worldhandle.DrawLine(rayWL.TheRay.Position, rayWL.TheRay.Direction, Color.Magenta);   
                }

            }
        }
    }
}
