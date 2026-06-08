using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using NUnit.Framework;
using Robust.Server.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Server.GameStates;

[TestFixture]
[TestOf(typeof(ChunkEntitySystem))]
public sealed partial class ChunkEntitySystemTest
{
    /// <summary>
    /// Ensures repeated requests for the same root/chunk pair return the same nullspace chunk entity.
    /// </summary>
    [Test]
    public void GetOrCreateReusesNullspaceChunkEntity()
    {
        var (sim, root) = SimulationWithGrid();
        var entMan = sim.Resolve<IEntityManager>();
        var chunks = entMan.System<ChunkEntitySystem>();

        var first = chunks.GetOrCreateChunk(root, new Vector2i(1, 2));
        var second = chunks.GetOrCreateChunk(root, new Vector2i(1, 2));
        var xform = entMan.GetComponent<TransformComponent>(first.Owner);

        Assert.Multiple(() =>
        {
            Assert.That(second.Owner, Is.EqualTo(first.Owner));
            Assert.That(first.Comp.Root, Is.EqualTo(root));
            Assert.That(first.Comp.Chunk, Is.EqualTo(new Vector2i(1, 2)));
            Assert.That(xform.MapID, Is.EqualTo(MapId.Nullspace));
        });
    }

    /// <summary>
    /// Ensures creating a chunk entity registers it as the attached payload entity for the matching PVS chunk.
    /// </summary>
    [Test]
    public void RegistersChunkEntityInPvsChunk()
    {
        var (sim, root) = SimulationWithGrid();
        var entMan = sim.Resolve<IEntityManager>();
        var chunks = entMan.System<ChunkEntitySystem>();
        var pvs = entMan.System<PvsSystem>();

        // Chunk entities are nullspace ents, but PVS should attach them to the matching spatial chunk.
        var chunk = chunks.GetOrCreateChunk(root, Vector2i.Zero);
        var pvsChunks = GetPvsChunks(pvs);

        var expected = new PvsChunkLocation(root, new Vector2i(0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(ChunkEntitySystem.ChunkSize, Is.EqualTo(16));
            Assert.That(PvsSystem.ChunkSize, Is.EqualTo(ChunkEntitySystem.ChunkSize));

            Assert.That(pvsChunks.TryGetValue(expected, out var pvsChunk), Is.True);
            Assert.That(pvsChunk!.AttachedChunkEntity, Is.EqualTo(chunk.Owner));
            Assert.That(pvsChunks.ContainsKey(new PvsChunkLocation(root, new Vector2i(1, 0))), Is.False);
        });
    }

    /// <summary>
    /// Ensures PVS chunks reject duplicate attached chunk entities for the same root/chunk pair.
    /// </summary>
    [Test]
    public void PvsChunkRejectsSecondAttachedChunkEntity()
    {
        var (sim, root) = SimulationWithGrid();
        var entMan = sim.Resolve<IEntityManager>();
        var chunks = entMan.System<ChunkEntitySystem>();

        chunks.GetOrCreateChunk(root, Vector2i.Zero);

        // There can only be one payload entity per root/chunk pair.
        var second = entMan.Spawn();
        var comp = new ChunkEntityComponent
        {
            Root = root,
            Chunk = Vector2i.Zero,
        };

        Assert.Throws<InvalidOperationException>(() => entMan.AddComponent(second, comp));
    }

    /// <summary>
    /// Ensures chunk enumeration returns existing chunk entities inside the requested local range only.
    /// </summary>
    [Test]
    public void EnumeratesExistingChunksInRange()
    {
        var (sim, root) = SimulationWithGrid();
        var entMan = sim.Resolve<IEntityManager>();
        var chunks = entMan.System<ChunkEntitySystem>();

        chunks.GetOrCreateChunk(root, new Vector2i(0, 0));
        chunks.GetOrCreateChunk(root, new Vector2i(1, 0));
        chunks.GetOrCreateChunk(root, new Vector2i(5, 0));

        var found = new HashSet<Vector2i>();
        var enumerator = chunks.GetChunksInRange(root, new Vector2(8, 8), 24);
        while (enumerator.MoveNext(out var chunk))
        {
            found.Add(chunk.Value.Comp.Chunk);
        }

        Assert.Multiple(() =>
        {
            Assert.That(found, Does.Contain(new Vector2i(0, 0)));
            Assert.That(found, Does.Contain(new Vector2i(1, 0)));
            Assert.That(found, Does.Not.Contain(new Vector2i(5, 0)));
        });
    }

    /// <summary>
    /// Ensures typed chunk enumeration only returns chunk entities that have the requested payload component.
    /// </summary>
    [Test]
    public void ComponentEnumeratorSkipsChunksWithoutComponent()
    {
        var (sim, root) = SimulationWithGrid();
        var entMan = sim.Resolve<IEntityManager>();
        var chunks = entMan.System<ChunkEntitySystem>();

        var withData = chunks.GetOrCreateChunk(root, new Vector2i(0, 0));
        chunks.GetOrCreateChunk(root, new Vector2i(1, 0));
        entMan.AddComponent<TestChunkDataComponent>(withData.Owner);

        var found = new List<EntityUid>();
        var query = entMan.GetEntityQuery<TestChunkDataComponent>();
        var enumerator = chunks.GetChunksInRange(root, new Vector2(8, 8), 24, query);
        while (enumerator.MoveNext(out var chunk))
        {
            found.Add(chunk.Value.Owner);
        }

        Assert.That(found, Is.EquivalentTo(new[] { withData.Owner }));
    }

    /// <summary>
    /// Ensures detached chunk entities remain registered internally but are hidden from public chunk lookups.
    /// </summary>
    [Test]
    public void TryGetChunkSkipsDetachedChunkEntity()
    {
        var (sim, root) = SimulationWithGrid();
        var entMan = sim.Resolve<IEntityManager>();
        var chunks = entMan.System<ChunkEntitySystem>();
        var meta = entMan.System<MetaDataSystem>();

        var chunk = chunks.GetOrCreateChunk(root, new Vector2i(0, 0));
        // Detached chunk entities still exist client-side, but must not be returned by public chunk lookups.
        meta.AddFlag(chunk.Owner, MetaDataFlags.Detached);

        var found = new List<EntityUid>();
        var enumerator = chunks.GetChunksInRange(root, new Vector2(8, 8), 24);
        while (enumerator.MoveNext(out var enumerated))
        {
            found.Add(enumerated.Value.Owner);
        }

        Assert.Multiple(() =>
        {
            Assert.That(chunks.TryGetChunk(root, new Vector2i(0, 0), out _), Is.False);
            Assert.That(found, Is.Empty);
        });
    }

    /// <summary>
    /// Ensures removing <see cref="ChunkEntityComponent"/> unregisters the chunk entity from both chunk lookup and PVS.
    /// </summary>
    [Test]
    public void RemovingChunkComponentUnregistersChunk()
    {
        var (sim, root) = SimulationWithGrid();
        var entMan = sim.Resolve<IEntityManager>();
        var chunks = entMan.System<ChunkEntitySystem>();
        var pvs = entMan.System<PvsSystem>();

        var chunk = chunks.GetOrCreateChunk(root, new Vector2i(0, 0));
        // Removing the marker component should unregister the chunk from both ChunkEntitySystem and PVS.
        entMan.RemoveComponent<ChunkEntityComponent>(chunk.Owner);

        var pvsChunks = GetPvsChunks(pvs);
        var location = new PvsChunkLocation(root, new Vector2i(0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(chunks.TryGetChunk(root, new Vector2i(0, 0), out _), Is.False);
            if (pvsChunks.TryGetValue(location, out var pvsChunk))
                Assert.That(pvsChunk.AttachedChunkEntity, Is.Null);
        });
    }

    /// <summary>
    /// Ensures chunk entities with payload components are retained when opportunistic cleanup is requested.
    /// </summary>
    [Test]
    public void TryRemoveChunkRetainsChunkWithPayload()
    {
        var (sim, root) = SimulationWithGrid();
        var entMan = sim.Resolve<IEntityManager>();
        var chunks = entMan.System<ChunkEntitySystem>();

        var chunk = chunks.GetOrCreateChunk(root, new Vector2i(0, 0));
        // Payload components mean the chunk still owns data, so it should not be deleted opportunistically.
        entMan.AddComponent<TestChunkDataComponent>(chunk.Owner);

        Assert.That(chunks.TryRemoveChunk(chunk), Is.False);
        Assert.That(entMan.Deleted(chunk.Owner), Is.False);
        Assert.That(chunks.TryGetChunk(root, new Vector2i(0, 0), out _), Is.True);
    }

    /// <summary>
    /// Ensures empty chunk entities are deleted when opportunistic cleanup is requested.
    /// </summary>
    [Test]
    public void TryRemoveChunkDeletesChunkWithoutPayload()
    {
        var (sim, root) = SimulationWithGrid();
        var entMan = sim.Resolve<IEntityManager>();
        var chunks = entMan.System<ChunkEntitySystem>();

        var chunk = chunks.GetOrCreateChunk(root, new Vector2i(0, 0));
        // Once the payload is gone, the empty chunk entity can be removed.
        entMan.AddComponent<TestChunkDataComponent>(chunk.Owner);
        entMan.RemoveComponent<TestChunkDataComponent>(chunk.Owner);

        Assert.That(chunks.TryRemoveChunk(chunk), Is.True);
        Assert.That(entMan.Deleted(chunk.Owner), Is.True);
        Assert.That(chunks.TryGetChunk(root, new Vector2i(0, 0), out _), Is.False);
    }

    /// <summary>
    /// Ensures deleting a grid deletes any chunk entities rooted on that grid.
    /// </summary>
    [Test]
    public void GridDeletionDeletesRelevantChunkEntities()
    {
        var (sim, root) = SimulationWithGrid();
        var entMan = sim.Resolve<IEntityManager>();
        var chunks = entMan.System<ChunkEntitySystem>();

        var chunk = chunks.GetOrCreateChunk(root, new Vector2i(0, 0));

        // Deleting a grid should clean up any chunk entities rooted on that grid.
        entMan.DeleteEntity(root);

        Assert.That(entMan.Deleted(chunk.Owner), Is.True);
    }

    /// <summary>
    /// Ensures deleting a map deletes any chunk entities rooted on that map.
    /// </summary>
    [Test]
    public void MapDeletionDeletesRelevantChunkEntities()
    {
        var sim = Simulation();
        var entMan = sim.Resolve<IEntityManager>();
        var chunks = entMan.System<ChunkEntitySystem>();
        var map = entMan.System<SharedMapSystem>().CreateMap();

        var chunk = chunks.GetOrCreateChunk(map, new Vector2i(0, 0));

        // Map-rooted chunk entities need the same cleanup path as grid-rooted ones.
        entMan.DeleteEntity(map);

        Assert.That(entMan.Deleted(chunk.Owner), Is.True);
    }

    private static (ISimulation Simulation, EntityUid Grid) SimulationWithGrid()
    {
        var sim = Simulation();
        var entMan = sim.Resolve<IEntityManager>();
        var map = entMan.System<SharedMapSystem>().CreateMap();
        var grid = sim.Resolve<IMapManager>().CreateGridEntity(map);
        return (sim, grid);
    }

    private static ISimulation Simulation()
    {
        var sim = RobustServerSimulation
            .NewSimulation()
            .RegisterComponents(factory => factory.RegisterClass<TestChunkDataComponent>())
            .InitializeInstance();

        var prototypes = sim.Resolve<IPrototypeManager>();
        prototypes.LoadString("""
        - type: entity
          id: ChunkEntity
          name: Chunk Entity
          save: false
          components:
          - type: Transform
            gridTraversal: false
        """);
        prototypes.ResolveResults();

        return sim;
    }

    private static Dictionary<PvsChunkLocation, PvsChunk> GetPvsChunks(PvsSystem pvs)
    {
        var field = typeof(PvsSystem).GetField("_chunks", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (Dictionary<PvsChunkLocation, PvsChunk>) field!.GetValue(pvs)!;
    }

    [RegisterComponent]
    private sealed partial class TestChunkDataComponent : Component;
}
