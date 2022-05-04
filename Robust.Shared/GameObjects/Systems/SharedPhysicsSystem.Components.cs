using System;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.GameObjects;

public partial class SharedPhysicsSystem
{
    private void OnPhysicsInit(EntityUid uid, PhysicsComponent component, ComponentInit args)
    {
        var xform = Transform(uid);

        if (xform.MapID != MapId.Nullspace)
        {
            var physicsMap = EntityManager.GetComponent<SharedPhysicsMapComponent>(MapManager.GetMapEntityId(xform.MapID));
            physicsMap.AddBody(component);

            if (component.BodyType != BodyType.Static &&
                (physicsMap.Gravity != Vector2.Zero ||
                 !component.LinearVelocity.Equals(Vector2.Zero) ||
                 !component.AngularVelocity.Equals(0f)))
            {
                component._awake = true;
            }
            else
            {
                component._awake = false;
            }

            if (component._awake)
                physicsMap.AddAwakeBody(component);
        }
        else
        {
            component._awake = false;
        }

        // Gets added to broadphase via fixturessystem
        var startup = new PhysicsInitializedEvent(uid);
        EntityManager.EventBus.RaiseLocalEvent(uid, ref startup);
    }

    public void SetLinearVelocity(PhysicsComponent body, Vector2 velocity)
    {
        if (body.BodyType == BodyType.Static ||
            !body.CanCollide) return;

        if (Vector2.Dot(velocity, velocity) > 0.0f)
            body.Awake = true;

        if (body._linearVelocity.EqualsApprox(velocity, 0.0001f))
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
