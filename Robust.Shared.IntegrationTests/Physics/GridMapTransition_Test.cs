using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
internal sealed class GridMapTransition_Test
{
    private const string FixtureId = "fix1";

    [Test]
    public void QueuedChildProxyIsRemovedWhenGridEntersNullspace()
    {
        var state = SetupGridChild();
        var childXform = state.EntManager.GetComponent<TransformComponent>(state.Child);
        var proxyId = state.Proxy.ProxyId;

        state.Xforms.DetachEntity(state.Grid, state.GridXform);

        AssertNoQueuedProxy(state);
        Assert.That(childXform.MapUid, Is.Null);
        Assert.That(childXform.Broadphase, Is.Null);
        Assert.That(state.Fixture.ProxyCount, Is.EqualTo(1));
        Assert.That(state.Fixture.Proxies[0].ProxyId, Is.EqualTo(proxyId));

        state.Broadphase.FindNewContacts();
    }

    [Test]
    public void GridCanEnterNullspaceAndThenBeDeletedWithQueuedChildProxy()
    {
        var state = SetupGridChild();

        state.Xforms.DetachEntity(state.Grid, state.GridXform);
        state.EntManager.DeleteEntity(state.Grid);

        AssertNoQueuedProxy(state);
        state.Broadphase.FindNewContacts();
    }

    [Test]
    public void GridReturningFromNullspaceRequeuesPreservedProxyOnce()
    {
        var state = SetupGridChild();
        var childXform = state.EntManager.GetComponent<TransformComponent>(state.Child);
        var proxyId = state.Proxy.ProxyId;

        state.Xforms.DetachEntity(state.Grid, state.GridXform);
        AssertNoQueuedProxy(state);

        state.Xforms.SetCoordinates(state.Grid, state.GridXform, new EntityCoordinates(state.MapA, Vector2.Zero));

        Assert.That(childXform.MapUid, Is.EqualTo(state.MapA));
        Assert.That(childXform.Broadphase, Is.EqualTo(new BroadphaseData(state.Grid, true, true)));
        Assert.That(state.Fixture.Proxies[0].ProxyId, Is.EqualTo(proxyId));
        AssertQueuedProxyOnce(state);

        state.Broadphase.FindNewContacts();
    }

    [Test]
    public void GridMovingBetweenMapsRequeuesChildProxyOnDestinationMap()
    {
        var state = SetupGridChild();
        var childXform = state.EntManager.GetComponent<TransformComponent>(state.Child);
        var proxyId = state.Proxy.ProxyId;

        state.Xforms.SetCoordinates(state.Grid, state.GridXform, new EntityCoordinates(state.MapB, Vector2.Zero));

        Assert.That(childXform.MapUid, Is.EqualTo(state.MapB));
        Assert.That(childXform.Broadphase, Is.EqualTo(new BroadphaseData(state.Grid, true, true)));
        Assert.That(state.Fixture.Proxies[0].ProxyId, Is.EqualTo(proxyId));
        AssertQueuedProxyOnce(state);

        state.Broadphase.FindNewContacts();
    }

    [Test]
    public void ChildLocalMoveAndGridMapChangeSameTickLeavesSingleDestinationProxy()
    {
        var state = SetupGridChild();
        var proxyId = state.Proxy.ProxyId;

        state.Xforms.SetLocalPosition(state.Child, new Vector2(0.25f, 0f));
        AssertQueuedProxyOnce(state);

        state.Xforms.SetCoordinates(state.Grid, state.GridXform, new EntityCoordinates(state.MapB, Vector2.Zero));

        Assert.That(state.Fixture.Proxies[0].ProxyId, Is.EqualTo(proxyId));
        Assert.That(state.EntManager.GetComponent<TransformComponent>(state.Child).MapUid, Is.EqualTo(state.MapB));
        AssertQueuedProxyOnce(state);

        state.Broadphase.FindNewContacts();
    }

    [Test]
    public void NestedChildrenUpdateCachedMapAndBroadphaseOnGridMapChange()
    {
        var state = SetupGridChild(nested: true);
        var parentXform = state.EntManager.GetComponent<TransformComponent>(state.Parent!.Value);
        var childXform = state.EntManager.GetComponent<TransformComponent>(state.Child);

        state.Xforms.SetCoordinates(state.Grid, state.GridXform, new EntityCoordinates(state.MapB, Vector2.Zero));

        Assert.That(parentXform.MapUid, Is.EqualTo(state.MapB));
        Assert.That(childXform.MapUid, Is.EqualTo(state.MapB));
        Assert.That(parentXform.Broadphase, Is.EqualTo(new BroadphaseData(state.Grid, false, false)));
        Assert.That(childXform.Broadphase, Is.EqualTo(new BroadphaseData(state.Grid, true, true)));
        AssertQueuedProxyOnce(state);
    }

    [Test]
    public void ContactsAreRecreatedOnDestinationMap()
    {
        var state = SetupGridChild(bodyType: BodyType.Dynamic, layer: 1, mask: 2);
        var other = CreatePhysicsEntity(state.Sim, state.MapBId, new Vector2(0.5f, 0.5f), BodyType.Static, layer: 2, mask: 1);
        var childBody = state.EntManager.GetComponent<PhysicsComponent>(state.Child);
        var otherBody = state.EntManager.GetComponent<PhysicsComponent>(other);

        state.Broadphase.FindNewContacts();
        Assert.That(childBody.ContactCount, Is.EqualTo(0));

        state.Xforms.SetCoordinates(state.Grid, state.GridXform, new EntityCoordinates(state.MapB, Vector2.Zero));
        AssertQueuedProxyOnce(state);

        state.Broadphase.FindNewContacts();

        Assert.That(childBody.ContactCount, Is.EqualTo(1));
        Assert.That(otherBody.ContactCount, Is.EqualTo(1));
    }

    [Test]
    public void StationaryGridNullspaceRoundTripPreservesProxyId()
    {
        var state = SetupGridChild();
        var proxyId = state.Proxy.ProxyId;

        state.Xforms.DetachEntity(state.Grid, state.GridXform);
        state.Xforms.SetCoordinates(state.Grid, state.GridXform, new EntityCoordinates(state.MapA, Vector2.Zero));

        Assert.That(state.Fixture.ProxyCount, Is.EqualTo(1));
        Assert.That(state.Fixture.Proxies[0].ProxyId, Is.EqualTo(proxyId));
        AssertQueuedProxyOnce(state);
    }

    private static TestState SetupGridChild(
        bool nested = false,
        BodyType bodyType = BodyType.Static,
        int layer = 1,
        int mask = 1)
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var maps = entManager.System<SharedMapSystem>();
        var xforms = entManager.System<SharedTransformSystem>();
        var broadphase = entManager.System<SharedBroadphaseSystem>();
        var physics = entManager.System<SharedPhysicsSystem>();
        var fixtures = entManager.System<FixtureSystem>();
        var (mapA, mapAId) = sim.CreateMap();
        var (mapB, mapBId) = sim.CreateMap();
        var grid = maps.CreateGridEntity(mapAId);

        maps.SetTile(grid, Vector2i.Zero, new Tile(1));

        EntityUid? parent = null;
        var fixtureParent = grid.Owner;
        if (nested)
        {
            parent = entManager.SpawnEntity(null, new EntityCoordinates(grid, Vector2.Zero));
            fixtureParent = parent.Value;
        }

        var child = entManager.SpawnEntity(null, new EntityCoordinates(fixtureParent, new Vector2(0.5f, 0.5f)));
        var body = entManager.AddComponent<PhysicsComponent>(child);
        physics.SetBodyType(child, bodyType, body: body);
        var shape = new PolygonShape();
        shape.SetAsBox(0.25f, 0.25f);
        fixtures.CreateFixture(child, FixtureId, new Fixture(shape, layer, mask, true), body: body);
        physics.SetCanCollide(child, true, body: body);

        var fixture = entManager.GetComponent<FixturesComponent>(child).Fixtures[FixtureId];
        var proxy = fixture.Proxies[0];

        Assert.That(fixture.ProxyCount, Is.EqualTo(1));
        Assert.That(physics.MoveBuffer, Does.Contain(proxy));

        return new TestState(
            sim,
            entManager,
            maps,
            xforms,
            broadphase,
            physics,
            mapA,
            mapAId,
            mapB,
            mapBId,
            grid,
            entManager.GetComponent<TransformComponent>(grid),
            parent,
            child,
            fixture,
            proxy);
    }

    private static EntityUid CreatePhysicsEntity(
        ISimulation sim,
        MapId mapId,
        Vector2 position,
        BodyType bodyType,
        int layer,
        int mask)
    {
        var entManager = sim.Resolve<IEntityManager>();
        var physics = entManager.System<SharedPhysicsSystem>();
        var fixtures = entManager.System<FixtureSystem>();
        var uid = entManager.SpawnEntity(null, new MapCoordinates(position, mapId));
        var body = entManager.AddComponent<PhysicsComponent>(uid);
        physics.SetBodyType(uid, bodyType, body: body);
        var shape = new PolygonShape();
        shape.SetAsBox(0.25f, 0.25f);
        fixtures.CreateFixture(uid, FixtureId, new Fixture(shape, layer, mask, true), body: body);
        physics.SetCanCollide(uid, true, body: body);
        return uid;
    }

    private static void AssertNoQueuedProxy(TestState state)
    {
        Assert.That(state.Physics.MoveBuffer, Does.Not.Contain(state.Proxy));
    }

    private static void AssertQueuedProxyOnce(TestState state)
    {
        Assert.That(state.Physics.MoveBuffer, Does.Contain(state.Proxy));
        Assert.That(state.Physics.MoveBuffer.Count(proxy => ReferenceEquals(proxy, state.Proxy)), Is.EqualTo(1));
    }

    private sealed record TestState(
        ISimulation Sim,
        IEntityManager EntManager,
        SharedMapSystem Maps,
        SharedTransformSystem Xforms,
        SharedBroadphaseSystem Broadphase,
        SharedPhysicsSystem Physics,
        EntityUid MapA,
        MapId MapAId,
        EntityUid MapB,
        MapId MapBId,
        Entity<MapGridComponent> Grid,
        TransformComponent GridXform,
        EntityUid? Parent,
        EntityUid Child,
        Fixture Fixture,
        FixtureProxy Proxy);
}
