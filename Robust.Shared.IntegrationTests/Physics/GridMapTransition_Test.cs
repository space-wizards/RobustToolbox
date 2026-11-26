using System.Linq;
using System.Numerics;
using Moq;
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
    public void RemovingBroadphaseReleasesAllOwnedFixtures([Values] bool detached)
    {
        var state = SetupGridChild(nested: true);
        var broadphase = state.EntManager.GetComponent<BroadphaseComponent>(state.Grid);
        var chain = new ChainShape();
        chain.CreateLoop(new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) });
        var added = new Fixture(chain, 1, 1, true);
        state.EntManager.System<FixtureSystem>().CreateFixture(state.Child, "added", added);
        var proxies = added.Proxies.ToArray();
        Assert.That(proxies.Length, Is.GreaterThan(1));
        if (detached)
            state.Xforms.DetachEntity(state.Grid, state.GridXform);

        state.EntManager.RemoveComponent<BroadphaseComponent>(state.Grid);
        Assert.That(broadphase.StaticTree.Count, Is.Zero);
        Assert.That(broadphase.DynamicTree.Count, Is.Zero);
        Assert.That(state.Fixture.Proxies, Is.Empty);
        Assert.That(added.Proxies, Is.Empty);
        Assert.That(state.Fixture.ProxyTree, Is.Null);
        Assert.That(added.ProxyTree, Is.Null);
        AssertNoQueuedProxy(state);
        foreach (var proxy in proxies)
        {
            Assert.That(state.Physics.MoveBuffer, Does.Not.Contain(proxy));
        }

        Assert.That(state.EntManager.GetComponent<TransformComponent>(state.Child).Broadphase, Is.Null);
        state.Broadphase.FindNewContacts();
        state.EntManager.DeleteEntity(state.Grid);
    }

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

    [TestCase(BodyType.Static, false)]
    [TestCase(BodyType.Static, true)]
    [TestCase(BodyType.Dynamic, false)]
    [TestCase(BodyType.Dynamic, true)]
    public void StationaryGridNullspacePreservesProxyId(BodyType bodyType, bool differentMap)
    {
        var state = SetupGridChild(bodyType: bodyType);
        var proxyId = state.Proxy.ProxyId;

        state.Xforms.DetachEntity(state.Grid, state.GridXform);
        state.Xforms.SetCoordinates(state.Grid, state.GridXform,
            new EntityCoordinates(differentMap ? state.MapB : state.MapA, Vector2.Zero));

        Assert.That(state.Fixture.ProxyCount, Is.EqualTo(1));
        Assert.That(state.Fixture.Proxies[0], Is.SameAs(state.Proxy));
        Assert.That(state.Fixture.Proxies[0].ProxyId, Is.EqualTo(proxyId));
        AssertQueuedProxyOnce(state);
        state.Broadphase.FindNewContacts();
        state.EntManager.DeleteEntity(state.Grid);
    }

    [Test]
    public void DetachedBodyTypeChangeProxyTree([Values(BodyType.Static, BodyType.Dynamic)] BodyType before,
        [Values] bool differentMap, [Values] bool deleteGrid)
    {
        var after = before == BodyType.Static ? BodyType.Dynamic : BodyType.Static;
        var state = SetupGridChild(bodyType: before);
        var broadphase = state.EntManager.GetComponent<BroadphaseComponent>(state.Grid);
        var oldTree = before == BodyType.Static ? broadphase.StaticTree : broadphase.DynamicTree;
        var newTree = after == BodyType.Static ? broadphase.StaticTree : broadphase.DynamicTree;

        state.Xforms.DetachEntity(state.Grid, state.GridXform);
        state.Physics.SetBodyType(state.Child, after);
        state.Xforms.SetCoordinates(state.Grid, state.GridXform,
            new EntityCoordinates(differentMap ? state.MapB : state.MapA, Vector2.Zero));

        var proxy = state.Fixture.Proxies.Single();
        Assert.That(newTree.GetProxy(proxy.ProxyId), Is.SameAs(proxy), "Proxy not on the new tree");
        Assert.That(oldTree.Count, Is.Zero, "Old tree contains a dead fixture");
        state.Broadphase.FindNewContacts();
        state.EntManager.DeleteEntity(deleteGrid ? state.Grid.Owner : state.Child);
        Assert.That(newTree.Count, Is.Zero);
        state.EntManager.DeleteEntity(state.Grid);
    }

    [Test]
    public void DetachedReparentReleasesOldGridProxy([Values] bool deleteSourceBeforeReturn)
    {
        var state = SetupGridChild(nested: true);
        var oldBroadphase = state.EntManager.GetComponent<BroadphaseComponent>(state.Grid);
        var otherGrid = state.Maps.CreateGridEntity(state.MapBId);
        state.Maps.SetTile(otherGrid, Vector2i.Zero, new Tile(1));

        var otherParent = state.EntManager.SpawnAttachedTo(null, new EntityCoordinates(otherGrid, Vector2.Zero));
        var otherXform = state.EntManager.GetComponent<TransformComponent>(otherGrid);
        state.Xforms.DetachEntity(state.Grid, state.GridXform);
        state.Xforms.DetachEntity(otherGrid, otherXform);
        state.Xforms.SetCoordinates(state.Parent!.Value, new EntityCoordinates(otherParent, Vector2.Zero));
        if (deleteSourceBeforeReturn)
        {
            state.EntManager.DeleteEntity(state.Grid);
            Assert.That(state.Fixture.ProxyCount, Is.Zero, "Disposing the owner must also release leaves");
            Assert.That(state.Fixture.ProxyTree, Is.Null);
        }
        state.Xforms.SetCoordinates(otherGrid, otherXform, new EntityCoordinates(state.MapB, Vector2.Zero));

        var proxy = state.Fixture.Proxies.Single();
        var newBroadphase = state.EntManager.GetComponent<BroadphaseComponent>(otherGrid);
        Assert.That(newBroadphase.StaticTree.GetProxy(proxy.ProxyId), Is.SameAs(proxy));
        Assert.That(oldBroadphase.StaticTree.Count, Is.Zero);
        state.EntManager.DeleteEntity(state.Grid);
        state.Broadphase.FindNewContacts();
        state.EntManager.DeleteEntity(otherGrid);
    }

    [Test]
    public void DeletingDetachedChildReleasesProxy(
        [Values(BodyType.Static, BodyType.Dynamic)] BodyType bodyType, [Values] bool changeType,
        [Values] bool deleteGrid)
    {
        var state = SetupGridChild(bodyType: bodyType);
        var broadphase = state.EntManager.GetComponent<BroadphaseComponent>(state.Grid);
        state.Xforms.DetachEntity(state.Grid, state.GridXform);
        if (changeType)
            state.Physics.SetBodyType(state.Child, bodyType == BodyType.Static ? BodyType.Dynamic : BodyType.Static);
        state.EntManager.DeleteEntity(deleteGrid ? state.Grid.Owner : state.Child);
        Assert.That(broadphase.StaticTree.Count, Is.Zero);
        Assert.That(broadphase.DynamicTree.Count, Is.Zero);
        Assert.That(state.Fixture.ProxyCount, Is.Zero);
        if (!deleteGrid)
            state.Xforms.SetCoordinates(state.Grid, state.GridXform, new EntityCoordinates(state.MapB, Vector2.Zero));
        state.Broadphase.FindNewContacts();
        state.EntManager.DeleteEntity(state.Grid);
    }

    [Test]
    public void DetachedFixtureReplacementReleasesOldProxy()
    {
        var state = SetupGridChild();
        var fixtures = state.EntManager.System<FixtureSystem>();
        var broadphase = state.EntManager.GetComponent<BroadphaseComponent>(state.Grid);
        state.Xforms.DetachEntity(state.Grid, state.GridXform);
        fixtures.DestroyFixture(state.Child, FixtureId);
        Assert.That(broadphase.StaticTree.Count, Is.Zero);
        Assert.That(state.Fixture.ProxyCount, Is.Zero);
        var replacement = new Fixture(new PhysShapeCircle(0.4f), 1, 1, true);
        fixtures.CreateFixture(state.Child, FixtureId, replacement);
        state.Physics.SetCanCollide(state.Child, true);
        Assert.That(replacement.ProxyCount, Is.Zero);
        state.Xforms.SetCoordinates(state.Grid, state.GridXform, new EntityCoordinates(state.MapB, Vector2.Zero));
        var proxy = replacement.Proxies.Single();
        Assert.That(broadphase.StaticTree.GetProxy(proxy.ProxyId), Is.SameAs(proxy));
        Assert.That(broadphase.StaticTree.Tree.ProxyCount, Is.EqualTo(1));
        AssertNoQueuedProxy(state);
        state.Broadphase.FindNewContacts();
        state.EntManager.DeleteEntity(state.Grid);
    }

    [Test]
    public void AddingDetachedFixturePreservesExistingProxy()
    {
        var state = SetupGridChild();
        state.Xforms.DetachEntity(state.Grid, state.GridXform);
        var added = new Fixture(new PhysShapeCircle(0.4f), 1, 1, true);
        state.EntManager.System<FixtureSystem>().CreateFixture(state.Child, "added", added);
        Assert.That(added.ProxyCount, Is.Zero);
        AssertNoQueuedProxy(state);
        state.Xforms.SetCoordinates(state.Grid, state.GridXform, new EntityCoordinates(state.MapB, Vector2.Zero));
        Assert.That(state.Fixture.Proxies.Single(), Is.SameAs(state.Proxy));
        var tree = state.EntManager.GetComponent<BroadphaseComponent>(state.Grid).StaticTree;
        var proxy = added.Proxies.Single();
        Assert.That(tree.GetProxy(proxy.ProxyId), Is.SameAs(proxy));
        Assert.That(tree.Tree.ProxyCount, Is.EqualTo(2));
        state.Broadphase.FindNewContacts();
        state.EntManager.DeleteEntity(state.Grid);
        Assert.That(tree.Count, Is.Zero);
    }

    [Test]
    public void DetachedShapeChangeUpdatesPreservedProxyBounds()
    {
        var state = SetupGridChild();
        state.Xforms.DetachEntity(state.Grid, state.GridXform);
        state.Physics.SetVertices(state.Child, FixtureId, state.Fixture, (PolygonShape) state.Fixture.Shape,
            new[] { new Vector2(-2, -2), new Vector2(2, -2), new Vector2(2, 2), new Vector2(-2, 2) });
        state.Xforms.SetCoordinates(state.Grid, state.GridXform, new EntityCoordinates(state.MapB, Vector2.Zero));
        var proxy = state.Fixture.Proxies.Single();
        Assert.That(proxy.AABB.Width, Is.GreaterThanOrEqualTo(4));
        var tree = state.EntManager.GetComponent<BroadphaseComponent>(state.Grid).StaticTree;
        Assert.That(tree.QueryPoint(new Vector2(2, 2)), Does.Contain(proxy));
        state.Broadphase.FindNewContacts();
        state.EntManager.DeleteEntity(state.Grid);
    }

    [Test]
    public void DisablingDetachedCollisionReleasesPreservedProxy()
    {
        var state = SetupGridChild();
        state.Xforms.DetachEntity(state.Grid, state.GridXform);
        state.Physics.SetCanCollide(state.Child, false);
        state.Xforms.SetCoordinates(state.Grid, state.GridXform, new EntityCoordinates(state.MapB, Vector2.Zero));
        var broadphase = state.EntManager.GetComponent<BroadphaseComponent>(state.Grid);
        Assert.That(broadphase.StaticTree.Count, Is.Zero);
        Assert.That(state.Fixture.ProxyCount, Is.Zero);
        state.Broadphase.FindNewContacts();
        state.EntManager.DeleteEntity(state.Grid);
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
            parent = entManager.SpawnAttachedTo(null, new EntityCoordinates(grid, Vector2.Zero));
            fixtureParent = parent.Value;
        }

        var child = entManager.SpawnAttachedTo(null, new EntityCoordinates(fixtureParent, new Vector2(0.5f, 0.5f)));
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
        var uid = entManager.Spawn(null, new MapCoordinates(position, mapId));
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
