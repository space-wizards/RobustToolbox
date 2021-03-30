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
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;

namespace Robust.Client.Debugging
{
    internal sealed class DebugPhysicsSystem : SharedDebugPhysicsSystem
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
                Vector2 normal;
                contact.GetWorldManifold(out normal, points);

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

        public PhysicsDebugOverlay(DebugPhysicsSystem system)
        {
            _physics = system;
        }

        protected internal override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
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
