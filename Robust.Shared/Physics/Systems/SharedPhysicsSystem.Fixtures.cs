using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    // Will get expanded as v3 gets ported but for now just handles fixture tracking.
    internal Fixture AddWorldFixture()
    {
        // P1 id pool + fixtures and figuring out a way to do it.
        //

        // TODO: Addddd solver sets
        // - Port cancollide

        var fixture = new Fixture();
        AddWorldFixture(fixture);
        return fixture;
    }

    internal void AddWorldFixture(Fixture fixture)
    {
        DebugTools.Assert(fixture.Id == 0);
        var id = _shapesPool.AllocId();

        if (id == Fixtures.Count)
        {
            Fixtures.Add(fixture);
        }
        else
        {
            DebugTools.Assert(Fixtures[id] == null);
            Fixtures[id] = fixture;
        }

        // Offset by 1 as slot 0 is also the default int (funny that).
        fixture.Id = id + 1;
    }

    internal void DestroyWorldFixture(Fixture fixture)
    {
        DebugTools.Assert(fixture.Id > 0);
        var id = fixture.Id - 1;
        Fixtures[id] = null;
        _shapesPool.FreeId(id);
        fixture.Id = 0;
    }

    public bool HasContact(Fixture fixtureA, Fixture fixtureB)
    {
        if (fixtureB.Id < fixtureA.Id)
        {
            (fixtureB, fixtureA) = (fixtureA, fixtureB);
        }

        var pairKey = GetPairKey(fixtureA.Id, fixtureB.Id);
        return _pairKeys.Contains(pairKey);
    }

    internal ulong GetPairKey(int idA, int idB)
    {
        DebugTools.Assert(idA > 0 && idB > 0);

        if (idA < idB)
        {
            return (ulong)idA << 32 | (uint)idB;
        }

        return (ulong)idB << 32 | (uint)idA;
    }

    public void SetDensity(EntityUid uid, string fixtureId, Fixture fixture, float value, bool update = true, FixturesComponent? manager = null)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture.Density.Equals(value))
            return;

        if (!Resolve(uid, ref manager))
            return;

        fixture.Density = value;

        if (update)
            _fixtureSystem.FixtureUpdate(uid, manager: manager);
    }

    public void SetFriction(EntityUid uid, string fixtureId, Fixture fixture, float value, bool update = true, FixturesComponent? manager = null)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture.Friction.Equals(value))
            return;

        if (!Resolve(uid, ref manager))
            return;

        fixture.Friction = value;

        if (update)
            _fixtureSystem.FixtureUpdate(uid, manager: manager);
    }

    public void SetHard(EntityUid uid, Fixture fixture, bool value, FixturesComponent? manager = null)
    {
        if (fixture.Hard.Equals(value))
            return;

        if (!Resolve(uid, ref manager))
            return;

        fixture.Hard = value;
        _fixtureSystem.FixtureUpdate(uid, manager: manager);
        WakeBody(uid);
    }

    public void SetRestitution(EntityUid uid, Fixture fixture, float value, bool update = true, FixturesComponent? manager = null)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture.Restitution.Equals(value))
            return;

        if (!Resolve(uid, ref manager))
            return;

        fixture.Restitution = value;

        if (update)
            _fixtureSystem.FixtureUpdate(uid, manager: manager);
    }

    /// <summary>
    /// Increases or decreases all fixtures of an entity in size by a certain factor.
    /// </summary>
    public void ScaleFixtures(Entity<FixturesComponent?> ent, float factor)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        foreach (var (id, fixture) in ent.Comp.Fixtures)
        {
            switch (fixture.Shape)
            {
                case EdgeShape edge:
                    SetVertices(ent, id, fixture,
                        edge,
                        edge.Vertex0 * factor,
                        edge.Vertex1 * factor,
                        edge.Vertex2 * factor,
                        edge.Vertex3 * factor, ent.Comp);
                    break;
                case PhysShapeCircle circle:
                    SetPositionRadius(ent, id, fixture, circle, circle.Position * factor, circle.Radius * factor, ent.Comp);
                    break;
                case PolygonShape poly:
                    var verts = poly.Vertices;

                    for (var i = 0; i < poly.VertexCount; i++)
                    {
                        verts[i] *= factor;
                    }

                    SetVertices(ent, id, fixture, poly, verts, ent.Comp);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    #region Collision Masks & Layers

    /// <summary>
    /// Similar to IsHardCollidable but also checks whether both entities are set to CanCollide
    /// </summary>
    public bool IsCurrentlyHardCollidable(Entity<FixturesComponent?, PhysicsComponent?> bodyA, Entity<FixturesComponent?, PhysicsComponent?> bodyB)
    {
        if (!_fixturesQuery.Resolve(bodyA, ref bodyA.Comp1, false) ||
            !_fixturesQuery.Resolve(bodyB, ref bodyB.Comp1, false) ||
            !PhysicsQuery.Resolve(bodyA, ref bodyA.Comp2, false) ||
            !PhysicsQuery.Resolve(bodyB, ref bodyB.Comp2, false))
        {
            return false;
        }

        if (!bodyA.Comp2.CanCollide ||
            !bodyB.Comp2.CanCollide)
        {
            return false;
        }

        return IsHardCollidable(bodyA, bodyB);
    }

    /// <summary>
    /// Returns true if both entities are hard-collidable with each other.
    /// </summary>
    public bool IsHardCollidable(Entity<FixturesComponent?, PhysicsComponent?> bodyA, Entity<FixturesComponent?, PhysicsComponent?> bodyB)
    {
        if (!_fixturesQuery.Resolve(bodyA, ref bodyA.Comp1, false) ||
            !_fixturesQuery.Resolve(bodyB, ref bodyB.Comp1, false) ||
            !PhysicsQuery.Resolve(bodyA, ref bodyA.Comp2, false) ||
            !PhysicsQuery.Resolve(bodyB, ref bodyB.Comp2, false))
        {
            return false;
        }

        // Fast check
        if (!bodyA.Comp2.Hard ||
            !bodyB.Comp2.Hard ||
            ((bodyA.Comp2.CollisionLayer & bodyB.Comp2.CollisionMask) == 0x0 &&
            (bodyA.Comp2.CollisionMask & bodyB.Comp2.CollisionLayer) == 0x0))
        {
            return false;
        }

        // Slow check
        foreach (var fix in bodyA.Comp1.Fixtures.Values)
        {
            if (!fix.Hard)
                continue;

            foreach (var other in bodyB.Comp1.Fixtures.Values)
            {
                if (!other.Hard)
                    continue;

                if ((fix.CollisionLayer & other.CollisionMask) == 0x0 &&
                    (fix.CollisionMask & other.CollisionLayer) == 0x0)
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    public void AddCollisionMask(EntityUid uid, string fixtureId, Fixture fixture, int mask, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionMask & mask) == mask) return;

        if (!Resolve(uid, ref manager))
            return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionMask |= mask;

        if (body != null || TryComp(uid, out body))
        {
            _fixtureSystem.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(uid, fixture);
    }

    public void SetCollisionMask(EntityUid uid, string fixtureId, Fixture fixture, int mask, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (fixture.CollisionMask == mask) return;

        if (!Resolve(uid, ref manager))
            return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionMask = mask;

        if (body != null || TryComp(uid, out body))
        {
            _fixtureSystem.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(uid, fixture);
    }

    public void RemoveCollisionMask(EntityUid uid, string fixtureId, Fixture fixture, int mask, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionMask & mask) == 0x0) return;

        if (!Resolve(uid, ref manager))
            return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionMask &= ~mask;

        if (body != null || TryComp(uid, out body))
        {
            _fixtureSystem.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(uid, fixture);
    }

    public void AddCollisionLayer(EntityUid uid, string fixtureId, Fixture fixture, int layer, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionLayer & layer) == layer) return;

        if (!Resolve(uid, ref manager))
            return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionLayer |= layer;

        if (body != null || TryComp(uid, out body))
        {
            _fixtureSystem.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(uid, fixture);
    }

    public void SetCollisionLayer(EntityUid uid, string fixtureId, Fixture fixture, int layer, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (fixture.CollisionLayer.Equals(layer))
            return;

        if (!Resolve(uid, ref manager))
            return;

        fixture.CollisionLayer = layer;

        if (body != null || TryComp(uid, out body))
        {
            _fixtureSystem.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(uid, fixture);
    }

    public void RemoveCollisionLayer(EntityUid uid, string fixtureId, Fixture fixture, int layer, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionLayer & layer) == 0x0 || !Resolve(uid, ref manager)) return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionLayer &= ~layer;

        if (body != null || TryComp(uid, out body))
        {
            _fixtureSystem.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(uid, fixture);
    }

    #endregion
}
