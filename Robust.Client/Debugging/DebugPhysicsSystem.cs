/*
* Farseer Physics Engine:
* Copyright (c) 2012 Ian Qvist
*
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org
*
* This software is provided 'as-is', without any express or implied
* warranty.  In no event will the authors be held liable for any damages
* arising from the use of this software.
* Permission is granted to anyone to use this software for any purpose,
* including commercial applications, and to alter it and redistribute it
* freely, subject to the following restrictions:
* 1. The origin of this software must not be misrepresented; you must not
* claim that you wrote the original software. If you use this software
* in a product, an acknowledgment in the product documentation would be
* appreciated but is not required.
* 2. Altered source versions must be plainly marked as such, and must not be
* misrepresented as being the original software.
* 3. This notice may not be removed or altered from any source distribution.
*/

/* Heavily inspired by Farseer */

using System;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;

namespace Robust.Client.Debugging
{
    public sealed class DebugPhysicsSystem : SharedDebugPhysicsSystem
    {
        /*
         * Used for debugging shapes, controllers, joints, contacts
         */

        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

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
                    IoCManager.Resolve<IOverlayManager>().AddOverlay(
                        new PhysicsDebugOverlay(
                            IoCManager.Resolve<IEyeManager>(),
                            this,
                            Get<SharedBroadphaseSystem>()));

                if (value == PhysicsDebugFlags.None)
                    IoCManager.Resolve<IOverlayManager>().RemoveOverlay(typeof(PhysicsDebugOverlay));

                _flags = value;
            }
        }

        private PhysicsDebugFlags _flags;

        public override void HandlePreSolve(Contact contact, in Manifold oldManifold)
        {
            if ((Flags & PhysicsDebugFlags.ContactPoints) != 0)
            {
                Manifold manifold = contact.Manifold;

                if (manifold.PointCount == 0)
                    return;

                Fixture fixtureA = contact.FixtureA!;

                PointState[] state1, state2;
                CollisionManager.GetPointStates(out state1, out state2, oldManifold, manifold);

                Span<Vector2> points = stackalloc Vector2[2];
                contact.GetWorldManifold(_physicsManager, out var normal, points);

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
    public enum PhysicsDebugFlags : byte
    {
        None = 0,
        ContactPoints = 1 << 0,
        ContactNormals = 1 << 1,
        Shapes = 1 << 2,
    }

    internal sealed class PhysicsDebugOverlay : Overlay
    {
        private IEyeManager _eyeManager = default!;
        private DebugPhysicsSystem _physics = default!;
        private SharedBroadphaseSystem _broadphaseSystem = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public PhysicsDebugOverlay(IEyeManager eyeManager, DebugPhysicsSystem system, SharedBroadphaseSystem broadphaseSystem)
        {
            _eyeManager = eyeManager;
            _physics = system;
            _broadphaseSystem = broadphaseSystem;
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            if (_physics.Flags == PhysicsDebugFlags.None) return;

            var viewport = _eyeManager.GetWorldViewport();
            var viewBounds = _eyeManager.GetWorldViewbounds();
            var worldHandle = args.WorldHandle;

            if ((_physics.Flags & PhysicsDebugFlags.Shapes) != 0 && !viewport.IsEmpty())
            {
                if (viewport.IsEmpty()) return;

                var mapId = _eyeManager.CurrentMap;

                foreach (var physBody in _broadphaseSystem.GetCollidingEntities(mapId, viewBounds))
                {
                    if (physBody.Owner.HasComponent<MapGridComponent>()) continue;

                    var xform = physBody.GetTransform();

                    const float AlphaModifier = 0.2f;

                    foreach (var fixture in physBody.Fixtures)
                    {
                        // Invalid shape - Box2D doesn't check for IsSensor
                        if (physBody.BodyType == BodyType.Dynamic && fixture.Mass == 0f)
                        {
                            DrawShape(worldHandle, fixture, xform, Color.Red.WithAlpha(AlphaModifier));
                        }
                        else if (!physBody.CanCollide)
                        {
                            DrawShape(worldHandle, fixture, xform, new Color(0.5f, 0.5f, 0.3f).WithAlpha(AlphaModifier));
                        }
                        else if (physBody.BodyType == BodyType.Static)
                        {
                            DrawShape(worldHandle, fixture, xform, new Color(0.5f, 0.9f, 0.5f).WithAlpha(AlphaModifier));
                        }
                        else if ((physBody.BodyType & (BodyType.Kinematic | BodyType.KinematicController)) != 0x0)
                        {
                            DrawShape(worldHandle, fixture, xform, new Color(0.5f, 0.5f, 0.9f).WithAlpha(AlphaModifier));
                        }
                        else if (!physBody.Awake)
                        {
                            DrawShape(worldHandle, fixture, xform, new Color(0.6f, 0.6f, 0.6f).WithAlpha(AlphaModifier));
                        }
                        else
                        {
                            DrawShape(worldHandle, fixture, xform, new Color(0.9f, 0.7f, 0.7f).WithAlpha(AlphaModifier));
                        }
                    }
                }
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

        private void DrawShape(DrawingHandleWorld worldHandle, Fixture fixture, Transform xform, Color color)
        {
            switch (fixture.Shape)
            {
                case PhysShapeCircle circle:
                    var center = Transform.Mul(xform, circle.Position);
                    worldHandle.DrawCircle(center, circle.Radius, color);
                    break;
                case EdgeShape edge:
                    var v1 = Transform.Mul(xform, edge.Vertex1);
                    var v2 = Transform.Mul(xform, edge.Vertex2);
                    worldHandle.DrawLine(v1, v2, color);

                    if (edge.OneSided)
                    {
                        worldHandle.DrawCircle(v1, 0.5f, color);
                        worldHandle.DrawCircle(v2, 0.5f, color);
                    }

                    break;
                case PolygonShape poly:
                    Span<Vector2> verts = stackalloc Vector2[poly.Vertices.Length];

                    for (var i = 0; i < verts.Length; i++)
                    {
                        verts[i] = Transform.Mul(xform, poly.Vertices[i]);
                    }

                    worldHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, color);
                    break;
                default:
                    return;
            }
        }
    }
}
