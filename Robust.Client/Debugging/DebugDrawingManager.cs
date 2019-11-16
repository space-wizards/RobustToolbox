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

        protected Dictionary<Ray, float> rays;
        private float timer = 0f;
        private float _rayLifeTime = 2f;
        private bool _debugDrawRays;
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
                    _overlayManager.AddOverlay(new DebugDrawRayOverlay(rays));
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
            rays = new Dictionary<Ray, float>();
        }

        private void HandleDrawRay(MsgRay msg)
        {
            var newRay = msg.RayToSend;
            rays.Add(newRay, timer + _rayLifeTime);
        }

        public void FrameUpdate(FrameEventArgs frameEventArgs)
        {
            if (!_debugDrawRays)
            {
                return;
            }
            timer += frameEventArgs.DeltaSeconds;
            var keysToRemove = new List<Ray>();
            foreach (var ray in rays)
            {
                if (ray.Value < timer)
                {
                    keysToRemove.Add(ray.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                rays.Remove(key);
            }
        }

        private sealed class DebugDrawRayOverlay : Overlay
        {
            public override OverlaySpace Space => OverlaySpace.WorldSpace;
            private Dictionary<Ray, float> rays;
            public DebugDrawRayOverlay(Dictionary<Ray,float> _rays) : base(nameof(DebugDrawRayOverlay))
            {
                rays = _rays;
            }
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                rays = null;
                rays = new Dictionary<Ray, float>();
            }
            protected override void Draw(DrawingHandleBase handle)
            {
                var worldhandle = (DrawingHandleBase)handle;
                foreach(var item in rays)
                {
                    var ray = item.Key;
                    worldhandle.DrawLine(ray.Position, ray.Direction, Color.Green);
                }

            }
        }
    }
}
