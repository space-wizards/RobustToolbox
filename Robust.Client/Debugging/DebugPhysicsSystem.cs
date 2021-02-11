using System;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;

// TODO: Copy farseer licence here coz it's heavily inspired by it.
namespace Robust.Client.Debugging
{
    internal sealed class DebugPhysicsSystem : EntitySystem
    {
        /*
         * Used for debugging shapes, controllers, joints, contacts
         */

        private const int MaxContactPoints = 2048;
        internal int PointCount;

        internal ContactPoint[] _points = new ContactPoint[MaxContactPoints];

        public PhysicsDebugFlags Flags
        {
            get => _flags;
            set
            {
                if (value == _flags) return;

                if (_flags == PhysicsDebugFlags.None)
                    IoCManager.Resolve<IOverlayManager>().AddOverlay(new PhysicsDebugOverlay(this));

                if (value == PhysicsDebugFlags.None)
                    IoCManager.Resolve<IOverlayManager>().RemoveOverlay(nameof(PhysicsDebugOverlay));

                _flags = value;
            }
        }

        private PhysicsDebugFlags _flags;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PreSolveMessage>(HandlePreSolve);
        }

        private void HandlePreSolve(PreSolveMessage message)
        {
            Contact contact = message.Contact;
            AetherManifold oldManifold = message.OldManifold;

            if ((Flags & PhysicsDebugFlags.ContactPoints) != 0)
            {
                AetherManifold manifold = contact.Manifold;

                if (manifold.PointCount == 0)
                    return;

                Fixture fixtureA = contact.FixtureA!;

                PointState[] state1, state2;
                CollisionManager.GetPointStates(out state1, out state2, oldManifold, manifold);

                Vector2[] points;
                Vector2 normal;
                contact.GetWorldManifold(out normal, out points);

                for (int i = 0; i < manifold.PointCount && PointCount < MaxContactPoints; ++i)
                {
                    if (fixtureA == null)
                        _points[i] = new ContactPoint();

                    ContactPoint cp = _points[PointCount];
                    cp.Position = points[i];
                    cp.Normal = normal;
                    cp.State = state2[i];
                    _points[PointCount] = cp;
                    ++PointCount;
                }
            }
        }

        internal struct ContactPoint
        {
            public Vector2 Normal;
            public Vector2 Position;
            public PointState State;
        }
    }

    [Flags]
    internal enum PhysicsDebugFlags : byte
    {
        None = 0,
        ContactPoints = 1 << 0,
        ContactNormals = 1 << 1,
        Shapes = 1 << 2,
    }

    internal sealed class PhysicsDebugOverlay : Overlay
    {
        private DebugPhysicsSystem _physics = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public PhysicsDebugOverlay(DebugPhysicsSystem system) : base(nameof(PhysicsDebugOverlay))
        {
            _physics = system;
        }

        protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
        {
            if (_physics.Flags == PhysicsDebugFlags.None) return;

            var worldHandle = (DrawingHandleWorld) handle;

            if ((_physics.Flags & PhysicsDebugFlags.Shapes) != 0)
            {
                // Port DebugDrawing over.
            }

            if ((_physics.Flags & PhysicsDebugFlags.ContactPoints) != 0)
            {
                const float axisScale = 0.3f;

                for (int i = 0; i < _physics.PointCount; ++i)
                {
                    DebugPhysicsSystem.ContactPoint point = _physics._points[i];

                    if (point.State == PointState.Add)
                        worldHandle.DrawCircle(point.Position, 0.5f, new Color(255, 77, 243, 77));
                    else if (point.State == PointState.Persist)
                        worldHandle.DrawCircle(point.Position, 0.5f, new Color(255, 77, 77, 77));

                    if ((_physics.Flags & PhysicsDebugFlags.ContactNormals) != 0)
                    {
                        Vector2 p1 = point.Position;
                        Vector2 p2 = p1 + point.Normal * axisScale;
                        worldHandle.DrawLine(p1, p2, new Color(255, 102, 230, 102));
                    }
                }

                _physics.PointCount = 0;
            }
        }
    }
}
