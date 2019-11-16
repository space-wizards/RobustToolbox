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

namespace Robust.Client.Debugging
{
    internal class DebugDrawingManager : IDebugDrawingManager
    {
#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _net;
        [Dependency] private readonly IOverlayManager _overlayManager;
#pragma warning restore 649

        private List<RayWithLifetime> raysWithLifeTime;
        private float timer = 0f;
        private float _rayLifeTime = 2f;
        private bool _debugDrawRays;

        private struct RayWithLifetime
        {
            public Ray TheRay;
            public float LifeTime;
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

        public float DebugRayLifetime
        {
            get => _rayLifeTime;
            set
            {
                if (value >= 0f)
                {
                    _rayLifeTime = value;
                }
            }
        }

        public void Initialize()
        {
            _net.RegisterNetMessage<MsgRay>(MsgRay.NAME, HandleDrawRay);
            raysWithLifeTime = new List<RayWithLifetime>();
        }

        private void HandleDrawRay(MsgRay msg)
        {
            var newRay = msg.RayToSend;
            var newRayWithLifetime = new RayWithLifetime
            {
                TheRay = newRay,
                LifeTime = timer + _rayLifeTime
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
            timer += frameEventArgs.DeltaSeconds;
            var keysToRemove = new List<RayWithLifetime>();
            foreach (var rayWL in raysWithLifeTime)
            {
                if (rayWL.LifeTime < timer)
                {
                    keysToRemove.Add(rayWL);
                }
            }

            foreach (var key in keysToRemove)
            {
                raysWithLifeTime.Remove(key);
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
                raysWithLifeTime = null;
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
