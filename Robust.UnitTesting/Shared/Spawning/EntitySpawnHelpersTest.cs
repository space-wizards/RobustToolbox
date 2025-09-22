using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Robust.UnitTesting.Shared.Spawning;

/// <summary>
/// This test checks that the various <see cref="IEntityManager"/> spawn helpers (e.g.,
/// <see cref="IEntityManager.TrySpawnNextTo"/>) work as intended.
/// </summary>
[TestFixture]
[Virtual]
public abstract partial class EntitySpawnHelpersTest : RobustIntegrationTest
{
    protected ServerIntegrationInstance Server = default!;
    protected IEntityManager EntMan = default!;
    protected SharedMapSystem MapSys = default!;
    protected SharedTransformSystem Xforms = default!;
    protected SharedContainerSystem Container = default!;

    // Even if unused, content / downstream tests might use this class, so removal would be a breaking change?
    protected IMapManager MapMan = default!; 

    protected EntityUid Map;
    protected MapId MapId;
    protected EntityUid Parent; // entity parented to the map.
    protected EntityUid ChildA; // in a container, inside _parent
    protected EntityUid ChildB; // in another container, inside _parent
    protected EntityUid GrandChildA; // in a container, inside _childA
    protected EntityUid GrandChildB; // attached to _childB, not directly in a container.
    protected EntityUid GreatGrandChildA; //  in a container, inside _grandChildA
    protected EntityUid GreatGrandChildB; //  in a container, inside _grandChildB

    protected EntityCoordinates ParentPos;
    protected EntityCoordinates GrandChildBPos;

    protected async Task Setup()
    {
        Server = StartServer();
        await Server.WaitIdleAsync();
        MapMan = Server.ResolveDependency<IMapManager>();
        EntMan = Server.ResolveDependency<IEntityManager>();
        MapSys = EntMan.System<SharedMapSystem>();
        Xforms = EntMan.System<SharedTransformSystem>();
        Container = EntMan.System<SharedContainerSystem>();

        // Set up map and spawn several nested containers
        await Server.WaitPost(() =>
        {
            Map = Server.System<SharedMapSystem>().CreateMap(out MapId);
            Parent = EntMan.SpawnEntity(null, new EntityCoordinates(Map, new(1,2)));
            ChildA = EntMan.SpawnEntity(null, new EntityCoordinates(Map, default));
            ChildB = EntMan.SpawnEntity(null, new EntityCoordinates(Map, default));
            GrandChildA = EntMan.SpawnEntity(null, new EntityCoordinates(Map, default));
            GrandChildB = EntMan.SpawnEntity(null, new EntityCoordinates(Map, default));
            GreatGrandChildA = EntMan.SpawnEntity(null, new EntityCoordinates(Map, default));
            GreatGrandChildB = EntMan.SpawnEntity(null, new EntityCoordinates(Map, default));
            Container.Insert(ChildA, Container.EnsureContainer<TestContainer>(Parent, "childA"));
            Container.Insert(ChildB, Container.EnsureContainer<TestContainer>(Parent, "childB"));
            Container.Insert(GrandChildA, Container.EnsureContainer<TestContainer>(ChildA, "grandChildA"));
            Xforms.SetCoordinates(GrandChildB, new EntityCoordinates(ChildB, new(2,1)));
            Container.Insert(GreatGrandChildA, Container.EnsureContainer<TestContainer>(GrandChildA, "greatGrandChildA"));
            Container.Insert(GreatGrandChildB, Container.EnsureContainer<TestContainer>(GrandChildB, "greatGrandChildB"));
        });
        await Server.WaitRunTicks(5);

        // Ensure transform hierarchy is as expected

        Assert.That(Xforms.GetParentUid(Parent), Is.EqualTo(Map));
        Assert.That(Xforms.GetParentUid(ChildA), Is.EqualTo(Parent));
        Assert.That(Xforms.GetParentUid(ChildB), Is.EqualTo(Parent));
        Assert.That(Xforms.GetParentUid(GrandChildA), Is.EqualTo(ChildA));
        Assert.That(Xforms.GetParentUid(GrandChildB), Is.EqualTo(ChildB));
        Assert.That(Xforms.GetParentUid(GreatGrandChildA), Is.EqualTo(GrandChildA));
        Assert.That(Xforms.GetParentUid(GreatGrandChildB), Is.EqualTo(GrandChildB));

        Assert.That(Container.IsEntityInContainer(Parent), Is.False);
        Assert.That(Container.IsEntityInContainer(ChildA));
        Assert.That(Container.IsEntityInContainer(ChildB));
        Assert.That(Container.IsEntityInContainer(GrandChildA));
        Assert.That(Container.IsEntityInContainer(GrandChildB), Is.False);
        Assert.That(Container.IsEntityOrParentInContainer(GrandChildB));
        Assert.That(Container.IsEntityInContainer(GreatGrandChildA));
        Assert.That(Container.IsEntityInContainer(GreatGrandChildB));

        Assert.That(Container.GetContainer(Parent, "childA").Contains(ChildA));
        Assert.That(Container.GetContainer(Parent, "childB").Contains(ChildB));
        Assert.That(Container.GetContainer(ChildA, "grandChildA").Contains(GrandChildA));
        Assert.That(Container.GetContainer(GrandChildA, "greatGrandChildA").Contains(GreatGrandChildA));
        Assert.That(Container.GetContainer(GrandChildB, "greatGrandChildB").Contains(GreatGrandChildB));

        ParentPos = EntMan.GetComponent<TransformComponent>(Parent).Coordinates;
        GrandChildBPos = EntMan.GetComponent<TransformComponent>(GrandChildB).Coordinates;

        Assert.That(ParentPos.Position, Is.EqualTo(new Vector2(1, 2)));
        Assert.That(GrandChildBPos.Position, Is.EqualTo(new Vector2(2, 1)));
    }

    /// <summary>
    /// Simple container that can store up to 2 entities.
    /// </summary>
    [SerializedType(nameof(TestContainer))]
    private sealed partial class TestContainer : BaseContainer
    {
        private readonly List<EntityUid> _ents = new();

        public override int Count => _ents.Count;

        public override IReadOnlyList<EntityUid> ContainedEntities => _ents;
        protected internal override void InternalInsert(EntityUid toInsert, IEntityManager entMan) => _ents.Add(toInsert);
        protected internal override void InternalRemove(EntityUid toRemove, IEntityManager entMan) => _ents.Remove(toRemove);
        public override bool Contains(EntityUid contained) => _ents.Contains(contained);
        protected internal override void InternalShutdown(IEntityManager entMan, SharedContainerSystem system, bool isClient) { }
        protected internal override bool CanInsert(EntityUid toinsert, bool assumeEmpty, IEntityManager entMan)
            => _ents.Count < 2 && !_ents.Contains(toinsert);
    }
}

