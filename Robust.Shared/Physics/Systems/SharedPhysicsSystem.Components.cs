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

namespace Robust.Shared.Physics.Systems;

public partial class SharedPhysicsSystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;

    #region Lifetime

    private void OnPhysicsInit(EntityUid uid, PhysicsComponent component, ComponentInit args)
    {
        var xform = Transform(uid);
        var manager = EnsureComp<FixturesComponent>(uid);

        if (component.CanCollide && (_containerSystem.IsEntityOrParentInContainer(uid) || xform.MapID == MapId.Nullspace))
        {
            SetCanCollide(uid, false, false, manager: manager, body: component);
        }

        if (component.CanCollide)
        {
            if (component.BodyType != BodyType.Static)
            {
                SetAwake(uid, component, true);
            }
        }

        // Gets added to broadphase via fixturessystem
        _fixtures.OnPhysicsInit(uid, manager, component);

        if (manager.FixtureCount == 0)
            component.CanCollide = false;

        var ev = new CollisionChangeEvent(component, component.CanCollide);
        RaiseLocalEvent(ref ev);
    }

    private void OnPhysicsGetState(EntityUid uid, PhysicsComponent component, ref ComponentGetState args)
    {
        args.State = new PhysicsComponentState(
            component.CanCollide,
            component.SleepingAllowed,
            component.FixedRotation,
            component.BodyStatus,
            component.LinearVelocity,
            component.AngularVelocity,
            component.BodyType,
            component._friction,
            component.LinearDamping,
            component.AngularDamping);
    }

    private void OnPhysicsHandleState(EntityUid uid, PhysicsComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PhysicsComponentState newState)
            return;

        SetSleepingAllowed(uid, component, newState.SleepingAllowed);
        SetFixedRotation(uid, newState.FixedRotation, body: component);
        SetCanCollide(uid, newState.CanCollide, body: component);
        component.BodyStatus = newState.Status;

        // So transform doesn't apply MapId in the HandleComponentState because ??? so MapId can still be 0.
        // Fucking kill me, please. You have no idea deep the rabbit hole of shitcode goes to make this work.
        TryComp<FixturesComponent>(uid, out var manager);

        SetLinearVelocity(uid, newState.LinearVelocity, body: component, manager: manager);
        SetAngularVelocity(uid, newState.AngularVelocity, body: component, manager: manager);
        SetBodyType(uid, newState.BodyType, manager, component);
        SetFriction(component, newState.Friction);
        SetLinearDamping(component, newState.LinearDamping);
        SetAngularDamping(component, newState.AngularDamping);
    }

    #endregion

    private bool IsMoveable(PhysicsComponent body)
    {
        return (body.BodyType & (BodyType.Dynamic | BodyType.KinematicController)) != 0x0;
    }

    #region Impulses

    public void ApplyAngularImpulse(EntityUid uid, float impulse, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (!Resolve(uid, ref body) || !IsMoveable(body) || !WakeBody(uid, manager: manager, body: body))
        {
            return;
        }

        SetAngularVelocity(uid, body.AngularVelocity + impulse * body.InvI, body: body);
    }

    public void ApplyForce(EntityUid uid, Vector2 force, Vector2 point, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (!Resolve(uid, ref body) || !IsMoveable(body) || !WakeBody(uid, manager: manager, body: body))
        {
            return;
        }

        body.Force += force;
        body.Torque += Vector2.Cross(point - body._localCenter, force);
        Dirty(body);
    }

    public void ApplyForce(EntityUid uid, Vector2 force, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (!Resolve(uid, ref body) || !IsMoveable(body) || !WakeBody(uid, manager: manager, body: body))
        {
            return;
        }

        body.Force += force;
        Dirty(body);
    }

    public void ApplyTorque(EntityUid uid, float torque, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (!Resolve(uid, ref body) || !IsMoveable(body) || !WakeBody(uid, manager: manager, body: body))
        {
            return;
        }

        body.Torque += torque;
        Dirty(body);
    }

    public void ApplyLinearImpulse(EntityUid uid, Vector2 impulse, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (!Resolve(uid, ref body) || !IsMoveable(body) || !WakeBody(uid, manager: manager, body: body))
        {
            return;
        }

        SetLinearVelocity(uid,body.LinearVelocity + impulse * body._invMass, body: body);
    }

    public void ApplyLinearImpulse(EntityUid uid, Vector2 impulse, Vector2 point, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (!Resolve(uid, ref body) || !IsMoveable(body) || !WakeBody(uid, manager: manager, body: body))
        {
            return;
        }

        SetLinearVelocity(uid, body.LinearVelocity + impulse * body._invMass, body: body);
        SetAngularVelocity(uid, body.AngularVelocity + body.InvI * Vector2.Cross(point - body._localCenter, impulse), body: body);
    }

    #endregion

    #region Setters

    public void DestroyContacts(PhysicsComponent body)
    {
        if (body.Contacts.Count == 0) return;

        var node = body.Contacts.First;

        while (node != null)
        {
            var contact = node.Value;
            node = node.Next;
            // Destroy last so the linked-list doesn't get touched.
            DestroyContact(contact);
        }

        DebugTools.Assert(body.Contacts.Count == 0);
    }

    /// <summary>
    /// Completely resets a dynamic body.
    /// </summary>
    public void ResetDynamics(PhysicsComponent body)
    {
        var updated = false;

        if (body.Torque != 0f)
        {
            body.Torque = 0f;
            updated = true;
        }

        if (body.AngularVelocity != 0f)
        {
            body.AngularVelocity = 0f;
            updated = true;
        }

        if (body.Force != Vector2.Zero)
        {
            body.Force = Vector2.Zero;
            updated = true;
        }

        if (body.LinearVelocity != Vector2.Zero)
        {
            body.LinearVelocity = Vector2.Zero;
            updated = true;
        }

        if (updated)
            Dirty(body);
    }

    public void ResetMassData(EntityUid uid, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (!Resolve(uid, ref manager, ref body))
            return;

        body._mass = 0.0f;
        body._invMass = 0.0f;
        body._inertia = 0.0f;
        body.InvI = 0.0f;
        var localCenter = Vector2.Zero;

        foreach (var fixture in manager.Fixtures.Values)
        {
            if (fixture.Density <= 0.0f) continue;

            var data = new MassData();
            FixtureSystem.GetMassData(fixture.Shape, ref data, fixture.Density);

            body._mass += data.Mass;
            localCenter += data.Center * data.Mass;
            body._inertia += data.I;
        }

        // Update this after re-calculating mass as content may want to use the sum of fixture masses instead.
        if (((int) body.BodyType & (int) (BodyType.Kinematic | BodyType.Static)) != 0)
        {
            body._localCenter = Vector2.Zero;
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

        if (body._inertia > 0.0f && !body.FixedRotation)
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
        body.LinearVelocity += Vector2.Cross(body.AngularVelocity, localCenter - oldCenter);
        Dirty(body);
    }

    public void SetAngularVelocity(EntityUid uid, float value, bool dirty = true, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return;

        if (body.BodyType == BodyType.Static)
            return;

        if (value * value > 0.0f)
        {
            if (!WakeBody(uid, manager: manager, body: body))
                return;
        }

        // CloseToPercent tolerance needs to be small enough such that an angular velocity just above
        // sleep-tolerance can damp down to sleeping.

        if (MathHelper.CloseToPercent(body.AngularVelocity, value, 0.00001f))
            return;

        body.AngularVelocity = value;

        if (dirty)
            Dirty(body);
    }

    /// <summary>
    /// Attempts to set the body to collidable, wake it, then move it.
    /// </summary>
    public void SetLinearVelocity(EntityUid uid, Vector2 velocity, bool dirty = true, bool wakeBody = true, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return;

        if (body.BodyType == BodyType.Static) return;

        if (wakeBody && Vector2.Dot(velocity, velocity) > 0.0f)
        {
            if (!WakeBody(uid, manager: manager, body: body))
                return;
        }

        if (body.LinearVelocity.EqualsApprox(velocity, 0.0000001f))
            return;

        body.LinearVelocity = velocity;

        if (dirty)
            Dirty(body);
    }

    public void SetAngularDamping(PhysicsComponent body, float value, bool dirty = true)
    {
        if (MathHelper.CloseTo(body.AngularDamping, value))
            return;

        body.AngularDamping = value;

        if (dirty)
            Dirty(body);
    }

    public void SetLinearDamping(PhysicsComponent body, float value, bool dirty = true)
    {
        if (MathHelper.CloseTo(body.LinearDamping, value))
            return;

        body.LinearDamping = value;

        if (dirty)
            Dirty(body);
    }

    public void SetAwake(EntityUid uid, PhysicsComponent body, bool value, bool updateSleepTime = true)
    {
        if (body.Awake == value)
            return;

        if (value && (body.BodyType == BodyType.Static || !body.CanCollide))
            return;

        body.Awake = value;

        if (value)
        {
            var ev = new PhysicsWakeEvent(uid, body);
            RaiseLocalEvent(uid, ref ev, true);
        }
        else
        {
            var ev = new PhysicsSleepEvent(uid, body);
            RaiseLocalEvent(uid, ref ev, true);
            ResetDynamics(body);
        }

        if (updateSleepTime)
            SetSleepTime(body, 0);

        Dirty(body);
    }

    public void TrySetBodyType(EntityUid uid, BodyType value, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)
    {
        if (Resolve(uid, ref body, ref manager, ref xform, false))
            SetBodyType(uid, value, manager, body, xform);
    }

    public void SetBodyType(EntityUid uid, BodyType value, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref body))
            return;

        if (body.BodyType == value)
            return;

        var oldType = body.BodyType;
        body.BodyType = value;
        ResetMassData(uid, manager, body);

        if (body.BodyType == BodyType.Static)
        {
            SetAwake(uid, body, false);
            body.LinearVelocity = Vector2.Zero;
            body.AngularVelocity = 0.0f;
        }
        // Even if it's dynamic if it can't collide then don't force it awake.
        else if (body.CanCollide)
        {
            SetAwake(uid, body, true);
        }

        body.Force = Vector2.Zero;
        body.Torque = 0.0f;

        _broadphase.RegenerateContacts(uid, body, manager, xform);

        if (body.Initialized)
        {
            var ev = new PhysicsBodyTypeChangedEvent(uid, body.BodyType, oldType, body);
            RaiseLocalEvent(uid, ref ev, true);
        }
    }

    public void SetBodyStatus(PhysicsComponent body, BodyStatus status, bool dirty = true)
    {
        if (body.BodyStatus == status)
            return;

        body.BodyStatus = status;

        if (dirty)
            Dirty(body);
    }

    /// <summary>
    /// Sets the <see cref="PhysicsComponent.CanCollide"/> property; this handles whether the body is enabled.
    /// </summary>
    /// <returns>CanCollide</returns>
    /// <param name="force">Bypasses fixture and container checks</param>
    public bool SetCanCollide(
        EntityUid uid,
        bool value,
        bool dirty = true,
        bool force = false,
        FixturesComponent? manager = null,
        PhysicsComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return false;

        if (body.CanCollide == value)
            return value;

        if (value)
        {
            if (!force)
            {
                // If we're recursively in a container then never set this.
                if (_containerSystem.IsEntityOrParentInContainer(uid))
                    return false;

                if (!Resolve(uid, ref manager) || manager.FixtureCount == 0 && !_mapMan.IsGrid(uid))
                    return false;
            }
            else
            {
                DebugTools.Assert(!_containerSystem.IsEntityOrParentInContainer(uid));
                DebugTools.Assert((Resolve(uid, ref manager) && manager.FixtureCount > 0) || _mapMan.IsGrid(uid));
            }
        }

        // Need to do this before SetAwake to avoid double-changing it.
        body.CanCollide = value;

        if (!value)
            SetAwake(uid, body, false);

        if (body.Initialized)
        {
            var ev = new CollisionChangeEvent(body, value);
            RaiseLocalEvent(ref ev);
        }

        if (dirty)
            Dirty(body);

        return value;
    }

    public void SetFixedRotation(EntityUid uid, bool value, bool dirty = true, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (!Resolve(uid, ref body) || body.FixedRotation == value)
            return;

        body.FixedRotation = value;
        body.AngularVelocity = 0.0f;
        ResetMassData(uid, manager: manager, body: body);

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

        if (body.BodyType != BodyType.Dynamic) return;

        if (MathHelper.CloseToPercent(body._inertia, value)) return;

        if (value > 0.0f && !body.FixedRotation)
        {
            body._inertia = value - body.Mass * Vector2.Dot(body._localCenter, body._localCenter);
            DebugTools.Assert(body._inertia > 0.0f);
            body.InvI = 1.0f / body._inertia;

            if (dirty)
                Dirty(body);
        }
    }

    public void SetLocalCenter(EntityUid uid, PhysicsComponent body, Vector2 value)
    {
        if (body.BodyType != BodyType.Dynamic) return;

        if (value.EqualsApprox(body._localCenter)) return;

        body._localCenter = value;
    }

    public void SetSleepingAllowed(EntityUid uid, PhysicsComponent body, bool value, bool dirty = true)
    {
        if (body.SleepingAllowed == value)
            return;

        if (!value)
            SetAwake(uid, body, true);

        body.SleepingAllowed = value;

        if (dirty)
            Dirty(body);
    }

    public void SetSleepTime(PhysicsComponent body, float value)
    {
        DebugTools.Assert(!float.IsNaN(value));

        if (MathHelper.CloseToPercent(value, body.SleepTime))
            return;

        body.SleepTime = value;
    }

    /// <summary>
    /// Tries to enable the body and also set it awake.
    /// </summary>
    /// <param name="force">Bypasses fixture and container checks</param>
    /// <returns>true if the body is collidable and awake</returns>
    public bool WakeBody(EntityUid uid, bool force = false, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (!SetCanCollide(uid, true, manager: manager, body: body, force: force) || !Resolve(uid, ref body))
            return false;

        SetAwake(uid, body, true);
        return body.Awake;
    }

    #endregion

    public Transform GetPhysicsTransform(EntityUid uid, TransformComponent? xform = null, EntityQuery<TransformComponent>? xformQuery = null)
    {
        if (!Resolve(uid, ref xform))
            return new Transform();

        xformQuery ??= GetEntityQuery<TransformComponent>();
        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform, xformQuery.Value);

        return new Transform(worldPos, worldRot);
    }

    /// <summary>
    /// Gets the physics World AABB, only considering fixtures.
    /// </summary>
    public Box2 GetWorldAABB(EntityUid uid, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref manager, ref body, ref xform))
            return new Box2();

        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform, GetEntityQuery<TransformComponent>());

        var transform = new Transform(worldPos, (float) worldRot.Theta);

        var bounds = new Box2(transform.Position, transform.Position);

        foreach (var fixture in manager.Fixtures.Values)
        {
            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                var boundy = fixture.Shape.ComputeAABB(transform, i);
                bounds = bounds.Union(boundy);
            }
        }

        return bounds;
    }

    public Box2 GetHardAABB(EntityUid uid, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref body, ref xform, ref manager))
        {
            return new Box2();
        }

        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform, GetEntityQuery<TransformComponent>());

        var transform = new Transform(worldPos, (float) worldRot.Theta);

        var bounds = new Box2(transform.Position, transform.Position);

        foreach (var fixture in manager.Fixtures.Values)
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

    public (int Layer, int Mask) GetHardCollision(EntityUid uid, FixturesComponent? manager = null)
    {
        if (!Resolve(uid, ref manager))
        {
            return (0, 0);
        }

        var layer = 0;
        var mask = 0;

        foreach (var fixture in manager.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            layer |= fixture.CollisionLayer;
            mask |= fixture.CollisionMask;
        }

        return (layer, mask);
    }

    public virtual void UpdateIsPredicted(EntityUid? uid, PhysicsComponent? physics = null)
    {
        // See client-side system
    }
}
