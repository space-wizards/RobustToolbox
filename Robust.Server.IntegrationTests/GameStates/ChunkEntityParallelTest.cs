using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Threading;

namespace Robust.UnitTesting.Server.GameStates;

[TestFixture]
[TestOf(typeof(ChunkEntitySystem))]
public sealed class ChunkEntityParallelTest
{
    [Test]
    public void GetOrCreateReusesNullspaceChunkEntity()
    {
        var (sim, root) = SimulationWithGrid();
        var entMan = sim.Resolve<IEntityManager>();
        var chunks = entMan.System<ChunkEntitySystem>();

        for (var x = 0; x < 10; x++)
        {
            for (var y = 0; y < 10; y++)
            {
                chunks.GetOrCreateChunk(root, new Vector2i(x, y));
            }
        }

        var jobs = new TestParallelJob[4];
        for (var i = 0; i < jobs.Length; i++)
        {
            jobs[i] = new TestParallelJob();
        }

        chunks.ProcessChunksParallelCardinal(root, jobs);

        var allJobChunksEnts = jobs
            .Select(j => j.Chunks)
            .ToArray();
        var allJobChunks = allJobChunksEnts
            .Select(l => l
                .Select(e => e.Comp.Chunk)
                .ToList()
            )
            .ToArray();

        for (var i = 0; i < jobs.Length; i++)
        {
            var jobEnts = allJobChunksEnts[i];
            var jobChunks = allJobChunks[i];
            Assert.That(jobEnts, Is.Not.Empty);

            foreach (var thisEnt in jobEnts)
            {
                var thisChunk = thisEnt.Comp.Chunk;
                var sameJobSameEnt = jobEnts.Where(other => other == thisEnt);
                var sameJobSameChunk = jobChunks.Where(other => other == thisChunk);

                // This entity and chunk was processed only once by this job
                Assert.That(sameJobSameEnt, Has.One.Items);
                Assert.That(sameJobSameChunk, Has.One.Items);

                // No cardinally-adjacent chunks were processed by this job
                Assert.That(jobChunks, Does.Not.Contain(thisChunk + Direction.North.ToIntVec()));
                Assert.That(jobChunks, Does.Not.Contain(thisChunk + Direction.East.ToIntVec()));
                Assert.That(jobChunks, Does.Not.Contain(thisChunk + Direction.South.ToIntVec()));
                Assert.That(jobChunks, Does.Not.Contain(thisChunk + Direction.West.ToIntVec()));

                // No other job processed the same chunk
                for (var j = 0; j < 4; j++)
                {
                    if (i == j)
                        continue;

                    Assert.That(allJobChunksEnts[j].Where(other => other == thisEnt), Is.Empty);
                    Assert.That(allJobChunks[j].Where(other => other == thisChunk), Is.Empty);
                }
            }
        }
    }

    private static (ISimulation Simulation, EntityUid Grid) SimulationWithGrid()
    {
        var sim = Simulation();
        var entMan = sim.Resolve<IEntityManager>();
        var maps = entMan.System<SharedMapSystem>();
        var map = maps.CreateMap();
        var grid = maps.CreateGridEntity(map);
        return (sim, grid);
    }

    private static ISimulation Simulation()
    {
        var sim = RobustServerSimulation
            .NewSimulation()
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

    private class TestParallelJob : IParallelRangeRobustJob, IChunkJob
    {
        public List<Entity<ChunkEntityComponent>> Chunks { get; set; } = null!;

        public void ExecuteRange(int startIndex, int endIndex)
        {
        }
    }
}
