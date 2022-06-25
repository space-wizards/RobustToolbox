using System;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public partial class SharedPhysicsSystem
{
    [Dependency] private readonly CollisionWakeSystem _collisionWakeSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
    private void OnPhysicsInit(EntityUid uid, PhysicsComponent component, ComponentInit args)
    {
        var xform = Transform(uid);

        if (component._canCollide && xform.MapID != MapId.Nullspace)
        {
            bool awake;
            component._awake = false;
            var physicsMap = EntityManager.GetComponent<SharedPhysicsMapComponent>(MapManager.GetMapEntityId(xform.MapID));

            if (component.BodyType != BodyType.Static &&
                (physicsMap.Gravity != Vector2.Zero ||
                 !component.LinearVelocity.Equals(Vector2.Zero) ||
                 !component.AngularVelocity.Equals(0f)))
            {
                awake = true;
            }
            else
            {
                awake = false;
            }

            if (awake)
                component.Awake = true;
        }
        else
        {
            component._awake = false;
        }

        // Gets added to broadphase via fixturessystem
        OnPhysicsInitialized(uid);

        // Issue the event for stuff that needs it.
        if (component._canCollide)
        {
            component._canCollide = false;
            component.CanCollide = true;
        }
    }

    private void OnPhysicsInitialized(EntityUid uid)
    {
        if (EntityManager.TryGetComponent(uid, out CollisionWakeComponent? wakeComp))
        {
            _collisionWakeSystem.OnPhysicsInit(uid, wakeComp);
        }
        _fixtureSystem.OnPhysicsInit(uid);
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
            component.BodyType);
    }

    private void OnPhysicsHandleState(EntityUid uid, PhysicsComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PhysicsComponentState newState)
            return;

        component.SleepingAllowed = newState.SleepingAllowed;
        component.FixedRotation = newState.FixedRotation;
        component.CanCollide = newState.CanCollide;
        component.BodyStatus = newState.Status;

        // So transform doesn't apply MapId in the HandleComponentState because ??? so MapId can still be 0.
        // Fucking kill me, please. You have no idea deep the rabbit hole of shitcode goes to make this work.

        Dirty(component);
        component.LinearVelocity = newState.LinearVelocity;
        component.AngularVelocity = newState.AngularVelocity;
        component.BodyType = newState.BodyType;
        component.Predict = false;
    }

    /// <summary>
    /// Attempts to set the body to collidable, wake it, then move it.
    /// </summary>
    /// <param name="body"></param>
    /// <param name="velocity"></param>
    public void SetLinearVelocity(PhysicsComponent body, Vector2 velocity)
    {
        if (body.BodyType == BodyType.Static) return;

        if (Vector2.Dot(velocity, velocity) > 0.0f)
            body.WakeBody();

        if (!body.CanCollide ||
            body._linearVelocity.EqualsApprox(velocity, 0.0001f))
            return;

        body._linearVelocity = velocity;
        Dirty(body);
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

    public void RecursiveDestroyContacts(PhysicsComponent body, MapId? mapId = null)
    {
        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        DestroyContacts(body, mapId, xformQuery.GetComponent(body.Owner));
        DoDestroy(xformQuery.GetComponent(body.Owner), bodyQuery, xformQuery, mapId);
    }

    private void DoDestroy(
        TransformComponent xform,
        EntityQuery<PhysicsComponent> bodyQuery,
        EntityQuery<TransformComponent> xformQuery,
        MapId? mapId = null)
    {

        var childEnumerator = xform.ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            var childXform = xformQuery.GetComponent(child.Value);

            if (bodyQuery.TryGetComponent(child.Value, out var body))
            {
                DestroyContacts(body, mapId, childXform);
            }

            DoDestroy(childXform, bodyQuery, xformQuery, mapId);
        }
    }

    public void DestroyContacts(PhysicsComponent body, MapId? mapId = null, TransformComponent? xform = null)
    {
        if (body.Contacts.Count == 0) return;

        xform ??= Transform(body.Owner);
        mapId ??= xform.MapID;

        if (!TryComp<SharedPhysicsMapComponent>(MapManager.GetMapEntityId(mapId.Value), out var map))
        {
            DebugTools.Assert(body.Contacts.Count == 0);
            return;
        }

        var node = body.Contacts.First;

        while (node != null)
        {
            var contact = node.Value;
            node = node.Next;
            // Destroy last so the linked-list doesn't get touched.
            map.ContactManager.Destroy(contact);
        }

        DebugTools.Assert(body.Contacts.Count == 0);
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
