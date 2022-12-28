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

using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using PhysicsComponent = Robust.Shared.Physics.Components.PhysicsComponent;

namespace Robust.Shared.Physics.Dynamics
{
    [RegisterComponent, NetworkedComponent]
    public sealed class PhysicsMapComponent : Component
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        internal SharedPhysicsSystem Physics = default!;

        public bool AutoClearForces;

        /// <summary>
        /// When substepping the client needs to know about the first position to use for lerping.
        /// </summary>
        public readonly Dictionary<EntityUid, (EntityUid ParentUid, Vector2 LocalPosition, Angle LocalRotation)>
            LerpData = new();

        /// <summary>
        /// Keep a buffer of everything that moved in a tick. This will be used to check for physics contacts.
        /// </summary>
        public readonly Dictionary<FixtureProxy, Box2> MoveBuffer = new();

        /// <summary>
        ///     Change the global gravity vector.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Gravity
        {
            get => _gravity;
            set
            {
                if (_gravity.EqualsApprox(value)) return;

                var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
                var bodyQuery = _entityManager.GetEntityQuery<PhysicsComponent>();

                // Force every body awake just in case.
                WakeBodiesRecursive(Owner, xformQuery, bodyQuery);

                _gravity = value;
            }
        }

        private Vector2 _gravity;

        private void WakeBodiesRecursive(EntityUid uid, EntityQuery<TransformComponent> xformQuery, EntityQuery<PhysicsComponent> bodyQuery)
        {
            if (bodyQuery.TryGetComponent(uid, out var body) &&
                body.BodyType == BodyType.Dynamic)
            {
                Physics.WakeBody(uid, body);
            }

            var xform = xformQuery.GetComponent(uid);
            var childEnumerator = xform.ChildEnumerator;

            while (childEnumerator.MoveNext(out var child))
            {
                WakeBodiesRecursive(child.Value, xformQuery, bodyQuery);
            }
        }

        /// <summary>
        ///     All awake bodies on this map.
        /// </summary>
        public readonly HashSet<PhysicsComponent> AwakeBodies = new();

        /// <summary>
        ///     Store last tick's invDT
        /// </summary>
        internal float _invDt0;

        public MapId MapId => _entityManager.GetComponent<TransformComponent>(Owner).MapID;

        #region AddRemove

        public void AddAwakeBody(PhysicsComponent body)
        {
            if (!body.CanCollide)
            {
                Logger.ErrorS("physics", $"Tried to add non-colliding {_entityManager.ToPrettyString(body.Owner)} as an awake body to map!");
                DebugTools.Assert(false);
                return;
            }

            if (body.BodyType == BodyType.Static)
            {
                Logger.ErrorS("physics", $"Tried to add static body {_entityManager.ToPrettyString(body.Owner)} as an awake body to map!");
                DebugTools.Assert(false);
                return;
            }

            DebugTools.Assert(body.Awake);
            AwakeBodies.Add(body);
        }

        public void RemoveSleepBody(PhysicsComponent body)
        {
            AwakeBodies.Remove(body);
        }

        #endregion
    }

    [ByRefEvent]
    public readonly struct PhysicsUpdateBeforeMapSolveEvent
    {
        public readonly bool Prediction;
        public readonly PhysicsMapComponent MapComponent;
        public readonly float DeltaTime;

        public PhysicsUpdateBeforeMapSolveEvent(bool prediction, PhysicsMapComponent mapComponent, float deltaTime)
        {
            Prediction = prediction;
            MapComponent = mapComponent;
            DeltaTime = deltaTime;
        }
    }

    [ByRefEvent]
    public readonly struct PhysicsUpdateAfterMapSolveEvent
    {
        public readonly bool Prediction;
        public readonly PhysicsMapComponent MapComponent;
        public readonly float DeltaTime;

        public PhysicsUpdateAfterMapSolveEvent(bool prediction, PhysicsMapComponent mapComponent, float deltaTime)
        {
            Prediction = prediction;
            MapComponent = mapComponent;
            DeltaTime = deltaTime;
        }
    }
}
