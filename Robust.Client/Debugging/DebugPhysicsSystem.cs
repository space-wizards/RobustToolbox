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

// MIT License

// Copyright (c) 2019 Erin Catto

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

/* Heavily inspired by Farseer */

using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Dynamics.Joints;

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

        internal ContactPoint[] Points = new ContactPoint[MaxContactPoints];

        public PhysicsDebugFlags Flags
        {
            get => _flags;
            set
            {
                if (value == _flags) return;

                if (_flags == PhysicsDebugFlags.None)
                    IoCManager.Resolve<IOverlayManager>().AddOverlay(
                        new PhysicsDebugOverlay(
                            EntityManager,
                            IoCManager.Resolve<IEyeManager>(),
                            IoCManager.Resolve<IInputManager>(),
                            IoCManager.Resolve<IResourceCache>(),
                            this,
                            Get<SharedPhysicsSystem>()));

                if (value == PhysicsDebugFlags.None)
                    IoCManager.Resolve<IOverlayManager>().RemoveOverlay(typeof(PhysicsDebugOverlay));

                _flags = value;
            }
        }

        private PhysicsDebugFlags _flags;

        public override void HandlePreSolve(Contact contact, in Manifold oldManifold)
        {
            if ((Flags & (PhysicsDebugFlags.ContactPoints | PhysicsDebugFlags.ContactNormals)) != 0)
            {
                Manifold manifold = contact.Manifold;

                if (manifold.PointCount == 0)
                    return;

                Fixture fixtureA = contact.FixtureA!;

                var state1 = new PointState[2];
                var state2 = new PointState[2];

                CollisionManager.GetPointStates(ref state1, ref state2, oldManifold, manifold);

                Span<Vector2> points = stackalloc Vector2[2];
                contact.GetWorldManifold(_physicsManager, out var normal, points);

                ContactPoint cp = Points[PointCount];
                for (var i = 0; i < manifold.PointCount && PointCount < MaxContactPoints; ++i)
                {
                    if (fixtureA == null)
                        Points[i] = new ContactPoint();

                    cp.Position = points[i];
                    cp.Normal = normal;
                    cp.State = state2[i];
                    Points[PointCount] = cp;
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
        /// <summary>
        /// Shows the world point for each contact in the viewport.
        /// </summary>
        ContactPoints = 1 << 0,
        /// <summary>
        /// Shows the world normal for each contact in the viewport.
        /// </summary>
        ContactNormals = 1 << 1,
        /// <summary>
        /// Shows all physics shapes in the viewport.
        /// </summary>
        Shapes = 1 << 2,
        ShapeInfo = 1 << 3,
        Joints = 1 << 4,
        AABBs = 1 << 5,
        COM = 1 << 6,
    }

    internal sealed class PhysicsDebugOverlay : Overlay
    {
        private IEntityManager _entityManager = default!;
        private IEyeManager _eyeManager = default!;
        private IInputManager _inputManager = default!;
        private DebugPhysicsSystem _debugPhysicsSystem = default!;
        private SharedPhysicsSystem _physicsSystem = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

        private static readonly Color JointColor = new(0.5f, 0.8f, 0.8f);

        private readonly Font _font;

        private HashSet<Joint> _drawnJoints = new();

        public PhysicsDebugOverlay(IEntityManager entityManager, IEyeManager eyeManager, IInputManager inputManager, IResourceCache cache, DebugPhysicsSystem system, SharedPhysicsSystem physicsSystem)
        {
            _entityManager = entityManager;
            _eyeManager = eyeManager;
            _inputManager = inputManager;
            _debugPhysicsSystem = system;
            _physicsSystem = physicsSystem;
            _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
        }

        private void DrawWorld(DrawingHandleWorld worldHandle, OverlayDrawArgs args)
        {
            var viewBounds = args.WorldBounds;
            var viewAABB = args.WorldAABB;
            var mapId = _eyeManager.CurrentMap;

            if ((_debugPhysicsSystem.Flags & PhysicsDebugFlags.Shapes) != 0)
            {
                foreach (var physBody in _physicsSystem.GetCollidingEntities(mapId, viewBounds))
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

            if ((_debugPhysicsSystem.Flags & PhysicsDebugFlags.COM) != 0)
            {
                foreach (var physBody in _physicsSystem.GetCollidingEntities(mapId, viewBounds))
                {
                    Color color;
                    const float Alpha = 0.25f;
                    float size;

                    if (physBody.Owner.HasComponent<MapGridComponent>())
                    {
                        color = Color.Orange.WithAlpha(Alpha);
                        size = 1f;
                    }
                    else
                    {
                        color = Color.Purple.WithAlpha(Alpha);
                        size = 0.2f;
                    }

                    var transform = physBody.GetTransform();

                    worldHandle.DrawCircle(Transform.Mul(transform, physBody.LocalCenter), size, color);
                }
            }

            if ((_debugPhysicsSystem.Flags & PhysicsDebugFlags.AABBs) != 0)
            {
                foreach (var physBody in _physicsSystem.GetCollidingEntities(mapId, viewBounds))
                {
                    if (physBody.Owner.HasComponent<MapGridComponent>()) continue;

                    var xform = physBody.GetTransform();

                    const float AlphaModifier = 0.2f;
                    Box2? aabb = null;

                    foreach (var fixture in physBody.Fixtures)
                    {
                        for (var i = 0; i < fixture.Shape.ChildCount; i++)
                        {
                            var shapeBB = fixture.Shape.ComputeAABB(xform, i);
                            aabb = aabb?.Union(shapeBB) ?? shapeBB;
                        }
                    }

                    if (aabb == null) continue;

                    worldHandle.DrawRect(aabb.Value, Color.Red.WithAlpha(AlphaModifier), false);
                }
            }

            if ((_debugPhysicsSystem.Flags & PhysicsDebugFlags.Joints) != 0x0)
            {
                _drawnJoints.Clear();

                foreach (var jointComponent in _entityManager.EntityQuery<JointComponent>(true))
                {
                    if (jointComponent.JointCount == 0 ||
                        !_entityManager.TryGetComponent(jointComponent.Owner.Uid, out TransformComponent? xf1) ||
                        !viewAABB.Contains(xf1.WorldPosition)) continue;

                    foreach (var (_, joint) in jointComponent.Joints)
                    {
                        if (_drawnJoints.Contains(joint)) continue;
                        DrawJoint(worldHandle, joint);
                        _drawnJoints.Add(joint);
                    }
                }
            }

            if ((_debugPhysicsSystem.Flags & (PhysicsDebugFlags.ContactPoints | PhysicsDebugFlags.ContactNormals)) != 0)
            {
                const float axisScale = 0.3f;

                for (var i = 0; i < _debugPhysicsSystem.PointCount; ++i)
                {
                    var point = _debugPhysicsSystem.Points[i];

                    const float radius = 0.1f;

                    if ((_debugPhysicsSystem.Flags & PhysicsDebugFlags.ContactPoints) != 0)
                    {
                        if (point.State == PointState.Add)
                            worldHandle.DrawCircle(point.Position, radius, new Color(255, 77, 243, 255));
                        else if (point.State == PointState.Persist)
                            worldHandle.DrawCircle(point.Position, radius, new Color(255, 77, 77, 255));
                    }

                    if ((_debugPhysicsSystem.Flags & PhysicsDebugFlags.ContactNormals) != 0)
                    {
                        Vector2 p1 = point.Position;
                        Vector2 p2 = p1 + point.Normal * axisScale;
                        worldHandle.DrawLine(p1, p2, new Color(255, 102, 230, 255));
                    }
                }

                _debugPhysicsSystem.PointCount = 0;
            }
        }

        private void DrawScreen(DrawingHandleScreen screenHandle, OverlayDrawArgs args)
        {
            var mapId = _eyeManager.CurrentMap;
            var mousePos = _inputManager.MouseScreenPosition;

            if ((_debugPhysicsSystem.Flags & PhysicsDebugFlags.ShapeInfo) != 0x0)
            {
                var hoverBodies = new List<PhysicsComponent>();
                var bounds = Box2.UnitCentered.Translated(_eyeManager.ScreenToMap(mousePos.Position).Position);

                foreach (var physBody in _physicsSystem.GetCollidingEntities(mapId, bounds))
                {
                    if (physBody.Owner.HasComponent<MapGridComponent>()) continue;
                    hoverBodies.Add(physBody);
                }

                var lineHeight = _font.GetLineHeight(1f);
                var drawPos = mousePos.Position + new Vector2(20, 0) + new Vector2(0, -(hoverBodies.Count * 4 * lineHeight / 2f));
                int row = 0;

                foreach (var body in hoverBodies)
                {
                    if (body != hoverBodies[0])
                    {
                        screenHandle.DrawString(_font, drawPos + new Vector2(0, row * lineHeight), "------");
                        row++;
                    }

                    screenHandle.DrawString(_font, drawPos + new Vector2(0, row * lineHeight), $"Ent: {body.Owner}");
                    row++;
                    screenHandle.DrawString(_font, drawPos + new Vector2(0, row * lineHeight), $"Layer: {Convert.ToString(body.CollisionLayer, 2)}");
                    row++;
                    screenHandle.DrawString(_font, drawPos + new Vector2(0, row * lineHeight), $"Mask: {Convert.ToString(body.CollisionMask, 2)}");
                    row++;
                    screenHandle.DrawString(_font, drawPos + new Vector2(0, row * lineHeight), $"Enabled: {body.CanCollide}, Hard: {body.Hard}, Anchored: {(body).BodyType == BodyType.Static}");
                    row++;
                }
            }
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            if (_debugPhysicsSystem.Flags == PhysicsDebugFlags.None) return;

            switch (args.Space)
            {
                case OverlaySpace.ScreenSpace:
                    DrawScreen((DrawingHandleScreen) args.DrawingHandle, args);
                    break;
                case OverlaySpace.WorldSpace:
                    DrawWorld((DrawingHandleWorld) args.DrawingHandle, args);
                    break;
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
                        worldHandle.DrawCircle(v1, 0.1f, color);
                        worldHandle.DrawCircle(v2, 0.1f, color);
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

        private void DrawJoint(DrawingHandleWorld worldHandle, Joint joint)
        {
            if (!_entityManager.TryGetComponent(joint.BodyAUid, out TransformComponent? xform1) ||
                !_entityManager.TryGetComponent(joint.BodyBUid, out TransformComponent? xform2)) return;

            var matrix1 = xform1.WorldMatrix;
            var matrix2 = xform2.WorldMatrix;

            var xf1 = new Vector2(matrix1.R0C2, matrix1.R1C2);
            var xf2 = new Vector2(matrix2.R0C2, matrix2.R1C2);

            var p1 = matrix1.Transform(joint.LocalAnchorA);
            var p2 = matrix2.Transform(joint.LocalAnchorB);

            var xfa = new Transform(xf1, xform1.WorldRotation);
            var xfb = new Transform(xf2, xform2.WorldRotation);

            switch (joint)
            {
                case DistanceJoint:
                    worldHandle.DrawLine(xf1, xf2, JointColor);
                    break;
                case PrismaticJoint prisma:
                    var pA = Transform.Mul(xfa, joint.LocalAnchorA);
                    var pB = Transform.Mul(xfb, joint.LocalAnchorB);

                    var axis = Transform.Mul(xfa.Quaternion2D, prisma._localXAxisA);

                    Color c1 = new(0.7f, 0.7f, 0.7f);
                    Color c2 = new(0.3f, 0.9f, 0.3f);
                    Color c3 = new(0.9f, 0.3f, 0.3f);
                    Color c4 = new(0.3f, 0.3f, 0.9f);
                    Color c5 = new(0.4f, 0.4f, 0.4f);

                    worldHandle.DrawLine(pA, pB, c5);

                    if (prisma.EnableLimit)
                    {
                        var lower = pA + axis * prisma.LowerTranslation;
                        var upper = pA + axis * prisma.UpperTranslation;
                        var perp = Transform.Mul(xfa.Quaternion2D, prisma._localYAxisA);
                        worldHandle.DrawLine(lower, upper, c1);
                        worldHandle.DrawLine(lower - perp * 0.5f, lower + perp * 0.5f, c2);
                        worldHandle.DrawLine(upper - perp * 0.5f, upper + perp * 0.5f, c3);
                    }
                    else
                    {
                        worldHandle.DrawLine(pA - axis * 1.0f, pA + axis * 1.0f, c1);
                    }

                    worldHandle.DrawCircle(pA, 0.5f, c1);
                    worldHandle.DrawCircle(pB, 0.5f, c4);
                    break;
                default:
                    worldHandle.DrawLine(xf1, p1, JointColor);
                    worldHandle.DrawLine(p1, p2, JointColor);
                    worldHandle.DrawLine(xf2, p2, JointColor);
                    break;
            }
        }
    }
}
