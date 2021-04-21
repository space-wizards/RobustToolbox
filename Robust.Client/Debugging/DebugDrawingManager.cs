using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;
using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Enums;
using Robust.Shared.Network;

namespace Robust.Client.Debugging
{
    internal class DebugDrawingManager : IDebugDrawingManager
    {
        [Dependency] private readonly IClientNetManager _net = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IGameTiming _gameTimer = default!;

        private readonly List<RayWithLifetime> raysWithLifeTime = new();
        private bool _debugDrawRays;

        private struct RayWithLifetime
        {
            public Vector2 RayOrigin;
            public Vector2 RayHit;
            public TimeSpan LifeTime;
            public bool DidActuallyHit;
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

                if (value && !_overlayManager.HasOverlay<DebugDrawRayOverlay>())
                {
                    _overlayManager.AddOverlay(new DebugDrawRayOverlay(this));
                }
                else
                {
                    _overlayManager.RemoveOverlay<DebugDrawRayOverlay>();
                }
            }
        }

        public TimeSpan DebugRayLifetime { get; set; } = TimeSpan.FromSeconds(5);

        public void Initialize()
        {
            _net.RegisterNetMessage<MsgRay>(MsgRay.NAME, HandleDrawRay);
        }

        private void HandleDrawRay(MsgRay msg)
        {
            // Rays are disposed with DebugDrawRayOverlay.FrameUpdate.
            // We don't accept incoming rays when that's off because else they'd pile up constantly.
            if (!_debugDrawRays)
            {
                return;
            }

            var newRayWithLifetime = new RayWithLifetime
            {
                DidActuallyHit = msg.DidHit,
                RayOrigin = msg.RayOrigin,
                RayHit = msg.RayHit,
                LifeTime = _gameTimer.RealTime + DebugRayLifetime
            };

            raysWithLifeTime.Add(newRayWithLifetime);
        }

        private sealed class DebugDrawRayOverlay : Overlay
        {
            private readonly DebugDrawingManager _owner;
            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            public DebugDrawRayOverlay(DebugDrawingManager owner)
            {
                _owner = owner;
            }

            protected internal override void Draw(in OverlayDrawArgs args)
            {
                var handle = args.WorldHandle;
                foreach (var ray in _owner.raysWithLifeTime)
                {
                    handle.DrawLine(
                        ray.RayOrigin,
                        ray.RayHit,
                        ray.DidActuallyHit ? Color.Yellow : Color.Magenta);
                }
            }

            protected internal override void FrameUpdate(FrameEventArgs args)
            {
                base.FrameUpdate(args);

                _owner.raysWithLifeTime.RemoveAll(r => r.LifeTime < _owner._gameTimer.RealTime);
            }
        }
    }
}
