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
 *
 * PhysicsComponent is heavily modified from Box2D.
*/

using System;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Utility;
using SharpZstd.Interop;

namespace Robust.Shared.Physics.Systems;

public partial class SharedPhysicsSystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;

    #region Lifetime

    private void OnPhysicsInit(EntityUid uid, PhysicsComponent component, ComponentInit args)
    {
        var xform = Transform(uid);


        if (component.CanCollide && (_containerSystem.IsEntityOrParentInContainer(uid) || xform.MapID == MapId.Nullspace))
        {
            SetCanCollide(component, false, false);
        }

        if (component._canCollide)
        {
            if (component.BodyType != BodyType.Static)
            {
                SetAwake(component, true);
            }
        }

        // Gets added to broadphase via fixturessystem
        var fixtures = EntityManager.EnsureComponent<FixturesComponent>(uid);
        _fixtures.OnPhysicsInit(uid, fixtures);

        if (fixtures.FixtureCount == 0)
            component._canCollide = false;

        var ev = new CollisionChangeEvent(component, component.CanCollide);
        RaiseLocalEvent(ref ev);
    }

    private void OnPhysicsGetState(EntityUid uid, PhysicsComponent component, ref ComponentGetState args)
    {
        args.State = new PhysicsComponentState(
            component._canCollide,
            component.SleepingAllowed,
            component.FixedRotation,
            component.BodyStatus,
            component.LinearVelocity,
            component.AngularVelocity,
            component.BodyType,
            component._friction,
            component._linearDamping,
            component._angularDamping);
    }

    private void OnPhysicsHandleState(EntityUid uid, PhysicsComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PhysicsComponentState newState)
            return;

        SetSleepingAllowed(component, newState.SleepingAllowed);
        SetFixedRotation(component, newState.FixedRotation);
        SetCanCollide(component, newState.CanCollide);
        component.BodyStatus = newState.Status;

        // So transform doesn't apply MapId in the HandleComponentState because ??? so MapId can still be 0.
        // Fucking kill me, please. You have no idea deep the rabbit hole of shitcode goes to make this work.

        SetLinearVelocity(component, newState.LinearVelocity);
        SetAngularVelocity(component, newState.AngularVelocity);
        SetBodyType(component, newState.BodyType);
        SetFriction(component, newState.Friction);
        SetLinearDamping(component, newState.LinearDamping);
        SetAngularDamping(component, newState.AngularDamping);
        component.Predict = false;
    }

    #endregion

    private bool IsMoveable(PhysicsComponent body)
    {
        return (body._bodyType & (BodyType.Dynamic | BodyType.KinematicController)) != 0x0;
    }

    #region Impulses

    public void ApplyAngularImpulse(PhysicsComponent body, float impulse)
    {
        if (!IsMoveable(body) || !WakeBody(body))
        {
            return;
        }

        SetAngularVelocity(body, body._angularVelocity + impulse * body.InvI);
    }

    public void ApplyForce(PhysicsComponent body, Vector2 force)
    {
        if (!IsMoveable(body) || !WakeBody(body))
        {
            return;
        }

        body.Force += force;
    }

    public void ApplyLinearImpulse(PhysicsComponent body, Vector2 impulse)
    {
        if (!IsMoveable(body) || !WakeBody(body))
        {
            return;
        }

        SetLinearVelocity(body, body._linearVelocity + impulse * body._invMass);
    }

    public void ApplyLinearImpulse(PhysicsComponent body, Vector2 impulse, Vector2 point)
    {
        if (!IsMoveable(body) || !WakeBody(body))
        {
            return;
        }

        SetLinearVelocity(body, body._linearVelocity + impulse * body._invMass);
        SetAngularVelocity(body, body._angularVelocity + body.InvI * Vector2.Cross(point - body._localCenter, impulse));
    }

    #endregion

    #region Setters

    public void DestroyContacts(PhysicsComponent body, MapId? mapId = null, TransformComponent? xform = null)
    {
        if (body.Contacts.Count == 0) return;

        xform ??= Transform(body.Owner);
        mapId ??= xform.MapID;

        if (!TryComp<SharedPhysicsMapComponent>(MapManager.GetMapEntityId(mapId.Value), out var map))
        {
            DebugTools.Assert("Attempted to destroy contacts, but entity has no physics map!");
            return;
        }

        DestroyContacts(body, map);
    }

    public void DestroyContacts(PhysicsComponent body, SharedPhysicsMapComponent physMap)
    {
        if (body.Contacts.Count == 0) return;

        var node = body.Contacts.First;

        while (node != null)
        {
            var contact = node.Value;
            node = node.Next;
            // Destroy last so the linked-list doesn't get touched.
            physMap.ContactManager.Destroy(contact);
        }

        DebugTools.Assert(body.Contacts.Count == 0);
    }

    /// <summary>
    /// Completely resets a dynamic body.
    /// </summary>
    public void ResetDynamics(PhysicsComponent body)
    {
        body.Torque = 0;
        body._angularVelocity = 0;
        body.Force = Vector2.Zero;
        body._linearVelocity = Vector2.Zero;
        Dirty(body);
    }

    public void ResetMassData(FixturesComponent fixtures, PhysicsComponent? body = null)
    {
        if (!Resolve(fixtures.Owner, ref body))
            return;

        ResetMassData(body, fixtures);
    }

    public void ResetMassData(PhysicsComponent body, FixturesComponent? fixtures = null)
    {
        body._mass = 0.0f;
        body._invMass = 0.0f;
        body._inertia = 0.0f;
        body.InvI = 0.0f;
        body._localCenter = Vector2.Zero;

        if (!Resolve(body.Owner, ref fixtures, false))
            return;

        var localCenter = Vector2.Zero;

        foreach (var (_, fixture) in fixtures.Fixtures)
        {
            if (fixture.Density <= 0.0f) continue;

            var data = new MassData();
            FixtureSystem.GetMassData(fixture.Shape, ref data, fixture.Density);

            body._mass += data.Mass;
            localCenter += data.Center * data.Mass;
            body._inertia += data.I;
        }

        // Update this after re-calculating mass as content may want to use the sum of fixture masses instead.
        if (((int) body._bodyType & (int) (BodyType.Kinematic | BodyType.Static)) != 0)
        {
            Dirty(body);
            return;
        }

        if (body._mass > 0.0f)
        {
            body._invMass = 1.0f / body._mass;
            localCenter *= body._invMass;
        }
        else
        {
            // Always need positive mass.
            body._mass = 1.0f;
            body._invMass = 1.0f;
        }

        if (body._inertia > 0.0f && !body._fixedRotation)
        {
            // Center inertia about center of mass.
            body._inertia -= body._mass * Vector2.Dot(localCenter, localCenter);

            DebugTools.Assert(body._inertia > 0.0f);
            body.InvI = 1.0f / body._inertia;
        }
        else
        {
            body._inertia = 0.0f;
            body.InvI = 0.0f;
        }

        var oldCenter = body._localCenter;
        body._localCenter = localCenter;

        // Update center of mass velocity.
        body._linearVelocity += Vector2.Cross(body._angularVelocity, localCenter - oldCenter);
        Dirty(body);
    }

    public void SetAngularVelocity(PhysicsComponent body, float value, bool dirty = true)
    {
        if (body.BodyType == BodyType.Static)
            return;

        if (value * value > 0.0f)
        {
            if (!WakeBody(body))
                return;
        }

        // CloseToPercent tolerance needs to be small enough such that an angular velocity just above
        // sleep-tolerance can damp down to sleeping.

        if (MathHelper.CloseToPercent(body._angularVelocity, value, 0.00001f))
            return;

        body._angularVelocity = value;

        if (dirty)
            Dirty(body);
    }

    /// <summary>
    /// Attempts to set the body to collidable, wake it, then move it.
    /// </summary>
    public void SetLinearVelocity(PhysicsComponent body, Vector2 velocity, bool dirty = true)
    {
        if (body.BodyType == BodyType.Static) return;

        if (Vector2.Dot(velocity, velocity) > 0.0f)
        {
            if (!WakeBody(body))
                return;
        }

        if (body._linearVelocity.EqualsApprox(velocity, 0.0001f))
            return;

        body._linearVelocity = velocity;

        if (dirty)
            Dirty(body);
    }

    public void SetAngularDamping(PhysicsComponent body, float value, bool dirty = true)
    {
        if (MathHelper.CloseTo(body._angularDamping, value))
            return;

        body._angularDamping = value;

        if (dirty)
            Dirty(body);
    }

    public void SetLinearDamping(PhysicsComponent body, float value, bool dirty = true)
    {
        if (MathHelper.CloseTo(body._linearDamping, value))
            return;

        body._linearDamping = value;

        if (dirty)
            Dirty(body);
    }

    public void SetAwake(PhysicsComponent body, bool value, bool updateSleepTime = true)
    {
        if (body._awake == value)
            return;

        if (value && (body.BodyType == BodyType.Static || !body.CanCollide))
            return;

        body._awake = value;

        if (value)
        {
            var ev = new PhysicsWakeEvent(body);
            RaiseLocalEvent(body.Owner, ref ev, true);
        }
        else
        {
            var ev = new PhysicsSleepEvent(body);
            RaiseLocalEvent(body.Owner, ref ev, true);
            ResetDynamics(body);
        }

        if (updateSleepTime)
            SetSleepTime(body, 0);

        Dirty(body);
    }

    public void TrySetBodyType(EntityUid uid, BodyType value)
    {
        if (TryComp(uid, out PhysicsComponent? body))
            SetBodyType(body, value);
    }

    public void SetBodyType(PhysicsComponent body, BodyType value)
    {
        if (body._bodyType == value)
            return;

        var oldType = body._bodyType;
        body._bodyType = value;
        ResetMassData(body);

        if (body._bodyType == BodyType.Static)
        {
            SetAwake(body, false);
            body._linearVelocity = Vector2.Zero;
            body._angularVelocity = 0.0f;
        }
        // Even if it's dynamic if it can't collide then don't force it awake.
        else if (body._canCollide)
        {
            SetAwake(body, true);
        }

        body.Force = Vector2.Zero;
        body.Torque = 0.0f;

        _broadphase.RegenerateContacts(body);

        if (body.Initialized)
        {
            var ev = new PhysicsBodyTypeChangedEvent(body.Owner, body._bodyType, oldType, body);
            RaiseLocalEvent(body.Owner, ref ev, true);
        }
    }


    /// <summary>
    /// Sets the <see cref="PhysicsComponent.CanCollide"/> property; this handles whether the body is enabled.
    /// </summary>
    /// <returns>CanCollide</returns>
    /// <param name="force">Bypasses fixture and container checks</param>
    public bool SetCanCollide(PhysicsComponent body, bool value, bool dirty = true, FixturesComponent? fixtures = null, bool force = false)
    {
        if (body._canCollide == value)
            return value;

        if (value)
        {
            if (!force)
            {
                // If we're recursively in a container then never set this.
                if (_containerSystem.IsEntityOrParentInContainer(body.Owner))
                    return false;

                if (Resolve(body.Owner, ref fixtures) && fixtures.FixtureCount == 0 && !_mapMan.IsGrid(body.Owner))
                    return false;
            }
            else
            {
                DebugTools.Assert(!_containerSystem.IsEntityOrParentInContainer(body.Owner));
                DebugTools.Assert((Resolve(body.Owner, ref fixtures) && fixtures.FixtureCount > 0) || _mapMan.IsGrid(body.Owner));
            }
        }

        // Need to do this before SetAwake to avoid double-changing it.
        body._canCollide = value;

        if (!value)
            SetAwake(body, false);

        if (body.Initialized)
        {
            var ev = new CollisionChangeEvent(body, value);
            RaiseLocalEvent(ref ev);
        }

        if (dirty)
            Dirty(body);

        return value;
    }

    public void SetFixedRotation(PhysicsComponent body, bool value, bool dirty = true)
    {
        if (body._fixedRotation == value)
            return;

        body._fixedRotation = value;
        body._angularVelocity = 0.0f;
        ResetMassData(body);

        if (dirty)
            Dirty(body);
    }

    public void SetFriction(PhysicsComponent body, float value, bool dirty = true)
    {
        if (MathHelper.CloseTo(body.Friction, value))
            return;

        body._friction = value;

        if (dirty)
            Dirty(body);
    }

    public void SetInertia(PhysicsComponent body, float value, bool dirty = true)
    {
        DebugTools.Assert(!float.IsNaN(value));

        if (body._bodyType != BodyType.Dynamic) return;

        if (MathHelper.CloseToPercent(body._inertia, value)) return;

        if (value > 0.0f && !body._fixedRotation)
        {
            body._inertia = value - body.Mass * Vector2.Dot(body._localCenter, body._localCenter);
            DebugTools.Assert(body._inertia > 0.0f);
            body.InvI = 1.0f / body._inertia;

            if (dirty)
                Dirty(body);
        }
    }

    public void SetLocalCenter(PhysicsComponent body, Vector2 value)
    {
        if (body._bodyType != BodyType.Dynamic) return;

        if (value.EqualsApprox(body._localCenter)) return;

        body._localCenter = value;
    }

    public void SetSleepingAllowed(PhysicsComponent body, bool value, bool dirty = true)
    {
        if (body._sleepingAllowed == value)
            return;

        if (!value)
            SetAwake(body, true);

        body._sleepingAllowed = value;

        if (dirty)
            Dirty(body);
    }

    public void SetSleepTime(PhysicsComponent body, float value)
    {
        DebugTools.Assert(!float.IsNaN(value));

        if (MathHelper.CloseToPercent(value, body._sleepTime))
            return;

        body._sleepTime = value;
    }

    public bool WakeBody(EntityUid uid, PhysicsComponent? body = null, FixturesComponent? manager = null, bool force = false)
    {
        if (!Resolve(uid, ref body, ref manager))
            return false;

        WakeBody(body, manager, force);
        return body._awake;
    }

    /// <summary>
    /// Tries to enable the body and also set it awake.
    /// </summary>
    /// <param name="force">Bypasses fixture and container checks</param>
    /// <returns>true if the body is collidable and awake</returns>
    public bool WakeBody(PhysicsComponent body, FixturesComponent? fixtures = null, bool force = false)
    {
        if (!SetCanCollide(body, true, fixtures: fixtures, force: force))
            return false;

        SetAwake(body, true);
        return body._awake;
    }

    #endregion

    public Transform GetPhysicsTransform(EntityUid uid, TransformComponent? xform = null, EntityQuery<TransformComponent>? xformQuery = null)
    {
        if (!Resolve(uid, ref xform))
            return new Transform();

        xformQuery ??= GetEntityQuery<TransformComponent>();
        var (worldPos, worldRot) = xform.GetWorldPositionRotation(xformQuery.Value);

        return new Transform(worldPos, worldRot);
    }

    public Box2 GetWorldAABB(PhysicsComponent body, TransformComponent xform, EntityQuery<TransformComponent> xforms, EntityQuery<FixturesComponent> fixtures)
    {
        var (worldPos, worldRot) = xform.GetWorldPositionRotation(xforms);

        var transform = new Transform(worldPos, (float) worldRot.Theta);

        var bounds = new Box2(transform.Position, transform.Position);

        foreach (var fixture in fixtures.GetComponent(body.Owner).Fixtures.Values)
        {
            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                var boundy = fixture.Shape.ComputeAABB(transform, i);
                bounds = bounds.Union(boundy);
            }
        }

        return bounds;
    }

    public Box2 GetHardAABB(PhysicsComponent body, TransformComponent? xform = null, FixturesComponent? fixtures = null)
    {
        if (!Resolve(body.Owner, ref xform, ref fixtures))
        {
            throw new InvalidOperationException();
        }

        var (worldPos, worldRot) = xform.GetWorldPositionRotation();

        var transform = new Transform(worldPos, (float) worldRot.Theta);

        var bounds = new Box2(transform.Position, transform.Position);

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!fixture.Hard) continue;

            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                var boundy = fixture.Shape.ComputeAABB(transform, i);
                bounds = bounds.Union(boundy);
            }
        }

        return bounds;
    }
}
