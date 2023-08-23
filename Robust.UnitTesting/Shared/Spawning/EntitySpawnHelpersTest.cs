using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.UnitTesting.Shared.Spawning;

/// <summary>
/// This test checks that the various <see cref="IEntityManager"/> spawn helpers (e.g.,
/// <see cref="IEntityManager.TrySpawnNextTo"/>) work as intended.
/// </summary>
[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class EntitySpawnHelpersTest : RobustIntegrationTest
{
    private ServerIntegrationInstance _server = default!;
    private IEntityManager _entMan = default!;
    private IMapManager _mapMan = default!;
    private SharedTransformSystem _xforms = default!;
    private SharedContainerSystem _container = default!;
    
    private EntityUid _map;
    private MapId _mapId;
    private EntityUid _parent; // entity parented to the map.
    private EntityUid _childA; // in a container, inside _parent
    private EntityUid _childB; // in another container, inside _parent
    private EntityUid _grandChildA; // in a container, inside _childA
    private EntityUid _grandChildB; // attached to _childB, not directly in a container.
    private EntityUid _greatGrandChildA; //  in a container, inside _grandChildA
    private EntityUid _greatGrandChildB; //  in a container, inside _grandChildB

    private EntityCoordinates _parentPos;
    private EntityCoordinates _grandChildBPos;
    
    [Test]
    public async Task TestTrySpawnNextTo()
    {
        await Setup();
        
        // Spawning next to an entity in a container will insert the entity into the container.
        await _server.WaitPost(() =>
        {
            Assert.That(_entMan.TrySpawnNextTo(null, _childA, out var uid));
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid!.Value), Is.EqualTo(_parent));
            Assert.That(_container.IsEntityInContainer(uid.Value));
            Assert.That(_container.GetContainer(_parent, "childA").Contains(uid.Value));
        });
        
        // The container is now full, spawning will fail.
        await _server.WaitPost(() =>
        {
            int count = _entMan.EntityCount;
            Assert.That(_entMan.TrySpawnNextTo(null, _childA, out var uid), Is.False);
            Assert.That(_entMan.EntityCount, Is.EqualTo(count));
            Assert.That(_entMan.EntityExists(uid), Is.False);
        });
        
        // Spawning next to an entity that is not in a container will simply spawn it in the same position
        await _server.WaitPost(() =>
        {
            Assert.That(_entMan.TrySpawnNextTo(null, _grandChildB, out var uid));
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid!.Value), Is.EqualTo(_childB));
            Assert.That(_container.IsEntityInContainer(uid.Value), Is.False);
            Assert.That(_container.IsEntityOrParentInContainer(uid.Value));
            Assert.That(_entMan.GetComponent<TransformComponent>(uid.Value).Coordinates, Is.EqualTo(_grandChildBPos));
        });
        
        // Spawning "next to" a nullspace entity will fail.
        await _server.WaitPost(() =>
        {
            int count = _entMan.EntityCount;
            Assert.That(_entMan.TrySpawnNextTo(null, _map, out var uid), Is.False);
            Assert.That(_entMan.EntityCount, Is.EqualTo(count));
            Assert.That(_entMan.EntityExists(uid), Is.False);
        });
        
        await _server.WaitPost(() =>_mapMan.DeleteMap(_mapId));
        _server.Dispose();
    }
    
    [Test]
    public async Task TestTrySpawnInContainer()
    {
        await Setup();
        
        // Spawning into a non-existent container does nothing.
        await _server.WaitPost(() =>
        {
            int count = _entMan.EntityCount;
            Assert.That(_entMan.TrySpawnInContainer(null, _childA, "foo", out var uid), Is.False);
            Assert.That(_entMan.EntityCount, Is.EqualTo(count));
            Assert.That(_entMan.EntityExists(uid), Is.False);
            Assert.That(_entMan.TrySpawnInContainer(null, _grandChildB, "foo", out uid), Is.False);
            Assert.That(_entMan.EntityCount, Is.EqualTo(count));
            Assert.That(_entMan.EntityExists(uid), Is.False);
        });
        
        // Spawning into a container works as expected.
        await _server.WaitPost(() =>
        {
            Assert.That(_entMan.TrySpawnInContainer(null, _childA, "grandChildA", out var uid));
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid!.Value), Is.EqualTo(_childA));
            Assert.That(_container.IsEntityInContainer(uid.Value));
            Assert.That(_container.GetContainer(_childA, "grandChildA").Contains(uid.Value));
        });
        
        // Spawning another entity will fail as the container is now full
        await _server.WaitPost(() =>
        {
            int count = _entMan.EntityCount;
            Assert.That(_entMan.TrySpawnInContainer(null, _childA, "grandChildA", out var uid), Is.False);
            Assert.That(_entMan.EntityCount, Is.EqualTo(count));
            Assert.That(_entMan.EntityExists(uid), Is.False);
        });
        
        await _server.WaitPost(() =>_mapMan.DeleteMap(_mapId));
        _server.Dispose();
    }
    
    [Test]
    public async Task TestSpawnNextToOrDrop()
    {
        await Setup();
        
        // Spawning next to an entity in a container will insert the entity into the container.
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnNextToOrDrop(null, _greatGrandChildA);
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_grandChildA));
            Assert.That(_container.IsEntityInContainer(uid));
            Assert.That(_container.GetContainer(_grandChildA, "greatGrandChildA").Contains(uid));
        });
        
        // The container is now full, spawning will insert into the outer container.
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnNextToOrDrop(null, _greatGrandChildA);
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_childA));
            Assert.That(_container.IsEntityInContainer(uid));
            Assert.That(_container.GetContainer(_childA, "grandChildA").Contains(uid));
        });
        
        // If outer two containers are full, will insert into outermost container.
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnNextToOrDrop(null, _greatGrandChildA);
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_parent));
            Assert.That(_container.IsEntityInContainer(uid));
            Assert.That(_container.GetContainer(_parent, "childA").Contains(uid));
        });
        
        // Finally, this will drop the item on the map.
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnNextToOrDrop(null, _greatGrandChildA);
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_map));
            Assert.That(_container.IsEntityInContainer(uid), Is.False);
            Assert.That(_entMan.GetComponent<TransformComponent>(uid).Coordinates, Is.EqualTo(_parentPos));
        });
        
        // Repeating this will just drop it on the map again.
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnNextToOrDrop(null, _greatGrandChildA);
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_map));
            Assert.That(_container.IsEntityInContainer(uid), Is.False);
            Assert.That(_entMan.GetComponent<TransformComponent>(uid).Coordinates, Is.EqualTo(_parentPos));
        });

        // Repeat the above but with the B-children. As _grandChildB is not actually IN a container, entities will
        // simply be parented to _childB.
        
        // First insert works fine
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnNextToOrDrop(null, _greatGrandChildB);
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_grandChildB));
            Assert.That(_container.IsEntityInContainer(uid));
            Assert.That(_container.GetContainer(_grandChildB, "greatGrandChildB").Contains(uid));
        });
        
        // Second insert will drop the entity next to _grandChildB
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnNextToOrDrop(null, _greatGrandChildB);
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_childB));
            Assert.That(_container.IsEntityInContainer(uid), Is.False);
            Assert.That(_entMan.GetComponent<TransformComponent>(uid).Coordinates, Is.EqualTo(_grandChildBPos));
        });
        
        // Repeating this will just repeat the above behaviour.
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnNextToOrDrop(null, _greatGrandChildB);
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_childB));
            Assert.That(_container.IsEntityInContainer(uid), Is.False);
            Assert.That(_entMan.GetComponent<TransformComponent>(uid).Coordinates, Is.EqualTo(_grandChildBPos));
        });
        
        // Spawning "next to" a map just drops the entity in nullspace
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnNextToOrDrop(null, _map);
            Assert.That(_entMan.EntityExists(uid));
            var xform = _entMan.GetComponent<TransformComponent>(uid);
            Assert.That(xform.ParentUid, Is.EqualTo(EntityUid.Invalid));
            Assert.That(xform.MapID, Is.EqualTo(MapId.Nullspace));
            Assert.Null(xform.MapUid);
            Assert.Null(xform.GridUid);
        });
        
        await _server.WaitPost(() =>_mapMan.DeleteMap(_mapId));
        _server.Dispose();
    }
    
    [Test]
    public async Task TestSpawnInContainerOrDrop()
    {
        await Setup();
        
        // Spawning next to an entity in a container will insert the entity into the container.
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnInContainerOrDrop(null, _grandChildA, "greatGrandChildA");
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_grandChildA));
            Assert.That(_container.IsEntityInContainer(uid));
            Assert.That(_container.GetContainer(_grandChildA, "greatGrandChildA").Contains(uid));
        });
        
        // The container is now full, spawning will insert into the outer container.
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnInContainerOrDrop(null, _grandChildA, "greatGrandChildA");
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_childA));
            Assert.That(_container.IsEntityInContainer(uid));
            Assert.That(_container.GetContainer(_childA, "grandChildA").Contains(uid));
        });
        
        // If outer two containers are full, will insert into outermost container.
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnInContainerOrDrop(null, _grandChildA, "greatGrandChildA");
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_parent));
            Assert.That(_container.IsEntityInContainer(uid));
            Assert.That(_container.GetContainer(_parent, "childA").Contains(uid));
        });
        
        // Finally, this will drop the item on the map.
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnInContainerOrDrop(null, _grandChildA, "greatGrandChildA");
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_map));
            Assert.That(_container.IsEntityInContainer(uid), Is.False);
            Assert.That(_entMan.GetComponent<TransformComponent>(uid).Coordinates, Is.EqualTo(_parentPos));
        });
        
        // Repeating this will just drop it on the map again.
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnInContainerOrDrop(null, _grandChildA, "greatGrandChildA");
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_map));
            Assert.That(_container.IsEntityInContainer(uid), Is.False);
            Assert.That(_entMan.GetComponent<TransformComponent>(uid).Coordinates, Is.EqualTo(_parentPos));
        });

        // Repeat the above but with the B-children. As _grandChildB is not actually IN a container, entities will
        // simply be parented to _childB.
        
        // First insert works fine
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnInContainerOrDrop(null, _grandChildB, "greatGrandChildB");
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_grandChildB));
            Assert.That(_container.IsEntityInContainer(uid));
            Assert.That(_container.GetContainer(_grandChildB, "greatGrandChildB").Contains(uid));
        });
        
        // Second insert will drop the entity next to _grandChildB
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnInContainerOrDrop(null, _grandChildB, "greatGrandChildB");
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_childB));
            Assert.That(_container.IsEntityInContainer(uid), Is.False);
            Assert.That(_entMan.GetComponent<TransformComponent>(uid).Coordinates, Is.EqualTo(_grandChildBPos));
        });
        
        // Repeating this will just repeat the above behaviour.
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnInContainerOrDrop(null, _grandChildB, "greatGrandChildB");
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_childB));
            Assert.That(_container.IsEntityInContainer(uid), Is.False);
            Assert.That(_entMan.GetComponent<TransformComponent>(uid).Coordinates, Is.EqualTo(_grandChildBPos));
        });
        
        // Trying to spawning inside a non-existent container just drops the entity
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnInContainerOrDrop(null, _grandChildB, "foo");
            Assert.That(_entMan.EntityExists(uid));
            Assert.That(_xforms.GetParentUid(uid), Is.EqualTo(_childB));
            Assert.That(_container.IsEntityInContainer(uid), Is.False);
            Assert.That(_entMan.GetComponent<TransformComponent>(uid).Coordinates, Is.EqualTo(_grandChildBPos));
        });
        
        // Trying to spawning "inside" a map just drops the entity in nullspace
        await _server.WaitPost(() =>
        {
            var uid = _entMan.SpawnInContainerOrDrop(null, _map, "foo");
            Assert.That(_entMan.EntityExists(uid));
            var xform = _entMan.GetComponent<TransformComponent>(uid);
            Assert.That(xform.ParentUid, Is.EqualTo(EntityUid.Invalid));
            Assert.That(xform.MapID, Is.EqualTo(MapId.Nullspace));
            Assert.Null(xform.MapUid);
            Assert.Null(xform.GridUid);
        });
        
        await _server.WaitPost(() =>_mapMan.DeleteMap(_mapId));
        _server.Dispose();
    }
    
    public async Task Setup()
    {
        _server = StartServer();
        await _server.WaitIdleAsync();
        _mapMan = _server.ResolveDependency<IMapManager>();
        _entMan = _server.ResolveDependency<IEntityManager>();
        _xforms = _entMan.System<SharedTransformSystem>();
        _container = _entMan.System<SharedContainerSystem>();

        // Set up map and spawn several nested containers
        await _server.WaitPost(() =>
        {
            _mapId = _mapMan.CreateMap();
            _map = _mapMan.GetMapEntityId(_mapId);
            _parent = _entMan.SpawnEntity(null, new EntityCoordinates(_map, new(1,2)));
            _childA = _entMan.SpawnEntity(null, new EntityCoordinates(_map, default));
            _childB = _entMan.SpawnEntity(null, new EntityCoordinates(_map, default));
            _grandChildA = _entMan.SpawnEntity(null, new EntityCoordinates(_map, default));
            _grandChildB = _entMan.SpawnEntity(null, new EntityCoordinates(_map, default));
            _greatGrandChildA = _entMan.SpawnEntity(null, new EntityCoordinates(_map, default));
            _greatGrandChildB = _entMan.SpawnEntity(null, new EntityCoordinates(_map, default));
            _container.EnsureContainer<TestContainer>(_parent, "childA").Insert(_childA);
            _container.EnsureContainer<TestContainer>(_parent, "childB").Insert(_childB);
            _container.EnsureContainer<TestContainer>(_childA, "grandChildA").Insert(_grandChildA);
            _xforms.SetCoordinates(_grandChildB, new EntityCoordinates(_childB, new(2,1)));
            _container.EnsureContainer<TestContainer>(_grandChildA, "greatGrandChildA").Insert(_greatGrandChildA);
            _container.EnsureContainer<TestContainer>(_grandChildB, "greatGrandChildB").Insert(_greatGrandChildB);
        });
        await _server.WaitRunTicks(5);
        
        // Ensure transform hierarchy is as expected
        
        Assert.That(_xforms.GetParentUid(_parent), Is.EqualTo(_map));
        Assert.That(_xforms.GetParentUid(_childA), Is.EqualTo(_parent));
        Assert.That(_xforms.GetParentUid(_childB), Is.EqualTo(_parent));
        Assert.That(_xforms.GetParentUid(_grandChildA), Is.EqualTo(_childA));
        Assert.That(_xforms.GetParentUid(_grandChildB), Is.EqualTo(_childB));
        Assert.That(_xforms.GetParentUid(_greatGrandChildA), Is.EqualTo(_grandChildA));
        Assert.That(_xforms.GetParentUid(_greatGrandChildB), Is.EqualTo(_grandChildB));
        
        Assert.That(_container.IsEntityInContainer(_parent), Is.False);
        Assert.That(_container.IsEntityInContainer(_childA));
        Assert.That(_container.IsEntityInContainer(_childB));
        Assert.That(_container.IsEntityInContainer(_grandChildA));
        Assert.That(_container.IsEntityInContainer(_grandChildB), Is.False);
        Assert.That(_container.IsEntityOrParentInContainer(_grandChildB));
        Assert.That(_container.IsEntityInContainer(_greatGrandChildA));
        Assert.That(_container.IsEntityInContainer(_greatGrandChildB));
        
        Assert.That(_container.GetContainer(_parent, "childA").Contains(_childA));
        Assert.That(_container.GetContainer(_parent, "childB").Contains(_childB));
        Assert.That(_container.GetContainer(_childA, "grandChildA").Contains(_grandChildA));
        Assert.That(_container.GetContainer(_grandChildA, "greatGrandChildA").Contains(_greatGrandChildA));
        Assert.That(_container.GetContainer(_grandChildB, "greatGrandChildB").Contains(_greatGrandChildB));

        _parentPos = _entMan.GetComponent<TransformComponent>(_parent).Coordinates;
        _grandChildBPos = _entMan.GetComponent<TransformComponent>(_grandChildB).Coordinates;
        
        Assert.That(_parentPos.Position, Is.EqualTo(new Vector2(1, 2)));
        Assert.That(_grandChildBPos.Position, Is.EqualTo(new Vector2(2, 1)));
    }
    
    /// <summary>
    /// Simple container that can store up to 2 entities.
    /// </summary>
    private sealed class TestContainer : BaseContainer
    {
        private readonly List<EntityUid> _ents = new();
        private readonly List<EntityUid> _expected = new();
        public override string ContainerType => nameof(TestContainer);
        public override IReadOnlyList<EntityUid> ContainedEntities => _ents;
        public override List<EntityUid> ExpectedEntities => _expected;
        protected override void InternalInsert(EntityUid toInsert, IEntityManager entMan) => _ents.Add(toInsert);
        protected override void InternalRemove(EntityUid toRemove, IEntityManager entMan) => _ents.Remove(toRemove);
        public override bool Contains(EntityUid contained) => _ents.Contains(contained);
        protected override void InternalShutdown(IEntityManager entMan, bool isClient) { }
        public override bool CanInsert(EntityUid toinsert, IEntityManager? entMan = null)
            => _ents.Count < 2 && !_ents.Contains(toinsert);
    }
}

