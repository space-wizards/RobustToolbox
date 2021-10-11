using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Robust.Client.Debugging
{
    internal sealed class DebugRayDrawingSystem : EntitySystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IGameTiming _gameTimer = default!;

        private readonly List<RayWithLifetime> _raysWithLifeTime = new();
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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<MsgRay>(HandleDrawRay);
            // To catch anything that's client-only and not sent by the server.
            SubscribeLocalEvent<DebugDrawRayMessage>(OnDebugDrawRay);
        }

        private void OnDebugDrawRay(DebugDrawRayMessage ev)
        {
            if (!_debugDrawRays)
            {
                return;
            }

            var newRayWithLifetime = new RayWithLifetime
            {
                DidActuallyHit = ev.Data.Results != null,
                RayOrigin = ev.Data.Ray.Position,
                RayHit = ev.Data.Results?.HitPos ?? ev.Data.Ray.Direction * ev.Data.MaxLength + ev.Data.Ray.Position,
                LifeTime = _gameTimer.RealTime + DebugRayLifetime
            };

            _raysWithLifeTime.Add(newRayWithLifetime);
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

            _raysWithLifeTime.Add(newRayWithLifetime);
        }

        private sealed class DebugDrawRayOverlay : Overlay
        {
            private readonly DebugRayDrawingSystem _owner;
            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            public DebugDrawRayOverlay(DebugRayDrawingSystem owner)
            {
                _owner = owner;
            }

            protected internal override void Draw(in OverlayDrawArgs args)
            {
                var handle = args.WorldHandle;
                foreach (var ray in _owner._raysWithLifeTime)
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

                _owner._raysWithLifeTime.RemoveAll(r => r.LifeTime < _owner._gameTimer.RealTime);
            }
        }
    }
}
