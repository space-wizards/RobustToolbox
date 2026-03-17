using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture, Parallelizable]
    sealed partial class EntityManagerTests
    {
        private static ISimulation SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .InitializeInstance();

            return sim;
        }

        /// <summary>
        /// The entity prototype can define field on the TransformComponent, just like any other component.
        /// </summary>
        [Test]
        public void SpawnEntity_PrototypeTransform_Works()
        {
            var sim = SimulationFactory();
            var map = sim.CreateMap().MapId;

            var entMan = sim.Resolve<IEntityManager>();
            var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, map));
            Assert.That(newEnt, Is.Not.EqualTo(EntityUid.Invalid));
        }

        [Test]
        [Description("""
        Tests that a three-component EntityQuery behaves as expected.
        This covers the backend for EntityQuery`2 and EntityQuery`4 as well due to shared code.
        """)]
        public void EntityQuery3_Works()
        {
            var sim = SimulationFactory();
            var map = sim.CreateMap();
            var map2 = sim.CreateMap();

            var entMan = sim.Resolve<IEntityManager>();
            // Query all maps.
            var query = entMan.GetEntityQuery<TransformComponent, MetaDataComponent, MapComponent>();

            Assert.That(query.Count(), NUnit.Framework.Is.EqualTo(2));

            Assert.That(query.Matches(map.Uid));

            foreach (var entity in query)
            {
                Assert.That(entity.Owner, NUnit.Framework.Is.EqualTo(map.Uid).Or.EqualTo(map2.Uid));
                Assert.That(entity.Comp1, NUnit.Framework.Is.TypeOf<TransformComponent>());
                Assert.That(entity.Comp2, NUnit.Framework.Is.TypeOf<MetaDataComponent>());
                Assert.That(entity.Comp3, NUnit.Framework.Is.TypeOf<MapComponent>());
#pragma warning disable CS0618 // Type or member is obsolete
                Assert.That(entity.Comp1.Owner, NUnit.Framework.Is.EqualTo(entity.Owner));
                Assert.That(entity.Comp2.Owner, NUnit.Framework.Is.EqualTo(entity.Owner));
                Assert.That(entity.Comp3.Owner, NUnit.Framework.Is.EqualTo(entity.Owner));
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        [Test]
        [Description("""
        Tests DynamicEntityQuery behavior with Without and Optional, ensuring they behave as expected.
        Uses a few maps and blank entities as test subjects.
        """)]
        public void DynamicQueryTest_OptionalWithout()
        {
            var sim = SimulationFactory();
            _ = sim.CreateMap();
            _ = sim.CreateMap();
            _ = sim.SpawnEntity(null, MapCoordinates.Nullspace);
            _ = sim.SpawnEntity(null, MapCoordinates.Nullspace);

            var entMan = sim.Resolve<IEntityManager>();

            // Should contain all spawned entities.
            var queryAll = entMan.GetDynamicQuery(
                (typeof(TransformComponent), DynamicEntityQuery.QueryFlags.None),
                (typeof(MetaDataComponent), DynamicEntityQuery.QueryFlags.None)
                );

            // Should contain all spawned entities.
            var queryAllAndMaps = entMan.GetDynamicQuery(
                (typeof(TransformComponent), DynamicEntityQuery.QueryFlags.None),
                (typeof(MetaDataComponent), DynamicEntityQuery.QueryFlags.None),
                (typeof(MapComponent), DynamicEntityQuery.QueryFlags.Optional)
            );

            // Should only contain the non-map entities.
            var queryNotMaps = entMan.GetDynamicQuery(
                (typeof(TransformComponent), DynamicEntityQuery.QueryFlags.None),
                (typeof(MetaDataComponent), DynamicEntityQuery.QueryFlags.None),
                (typeof(MapComponent), DynamicEntityQuery.QueryFlags.Without)
            );

            var buffer = new IComponent?[4].AsSpan();

            var queryAllEnum = queryAll.GetEnumerator(false);
            var queryAllCount = 0;

            while (queryAllEnum.MoveNext(out _, buffer[0..2]))
            {
                queryAllCount += 1;
                // Ensure components get filled out as we expect, only meta and transform.
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(buffer[0], NUnit.Framework.Is.TypeOf<TransformComponent>());
                    Assert.That(buffer[1], NUnit.Framework.Is.TypeOf<MetaDataComponent>());
                }
            }

            Assert.That(queryAllCount, NUnit.Framework.Is.EqualTo(4), "Expected to iterate all entities");

            var queryAllAndMapsEnum = queryAllAndMaps.GetEnumerator(false);
            var queryAllAndMapsCount = 0;
            var mapCount = 0;

            while (queryAllAndMapsEnum.MoveNext(out _, buffer[0..3]))
            {
                queryAllAndMapsCount += 1;
                using (Assert.EnterMultipleScope())
                {
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(buffer[0], NUnit.Framework.Is.TypeOf<TransformComponent>());
                        Assert.That(buffer[1], NUnit.Framework.Is.TypeOf<MetaDataComponent>());
                        Assert.That(buffer[2], NUnit.Framework.Is.TypeOf<MapComponent>().Or.Null);
                    }

                    if (buffer[2] is not null)
                        mapCount += 1;
                }
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(queryAllAndMapsCount, NUnit.Framework.Is.EqualTo(4), "Expected to iterate all entities.");
                Assert.That(mapCount, NUnit.Framework.Is.EqualTo(2), "Expected to pick up both maps in the query.");
            }

            var queryNotMapsEnum = queryNotMaps.GetEnumerator(false);
            var queryNotMapsCount = 0;

            while (queryNotMapsEnum.MoveNext(out var ent, buffer[0..3]))
            {
                queryNotMapsCount += 1;
                using (Assert.EnterMultipleScope())
                {
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(buffer[0], NUnit.Framework.Is.TypeOf<TransformComponent>());
                        Assert.That(buffer[1], NUnit.Framework.Is.TypeOf<MetaDataComponent>());
                        Assert.That(buffer[2], NUnit.Framework.Is.Null, "Without constraints should never fill their slot.");
                        Assert.That(entMan.HasComponent<MapComponent>(ent), NUnit.Framework.Is.False, "Without constraints shouldn't return entities that match it.");
                    }
                }
            }


            Assert.That(queryNotMapsCount, NUnit.Framework.Is.EqualTo(2), "Expected to only iterate non-maps.");
        }

        [Test]
        public void ComponentCount_Works()
        {
            var sim = RobustServerSimulation.NewSimulation().InitializeInstance();

            var entManager = sim.Resolve<IEntityManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            Assert.That(entManager.Count<TransformComponent>(), Is.EqualTo(0));

            var mapId = sim.CreateMap().MapId;
            Assert.That(entManager.Count<TransformComponent>(), Is.EqualTo(1));
            mapSystem.DeleteMap(mapId);
            Assert.That(entManager.Count<TransformComponent>(), Is.EqualTo(0));
        }
    }
}
