using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Server.Maps
{
    [TestFixture]
    public sealed partial class MapLoaderTest : RobustIntegrationTest
    {
        private const string MapData = @"
meta:
  format: 7
  category: Grid
  engineVersion: 238.0.0
  forkId: """"
  forkVersion: """"
  time: 12/22/2024 04:08:12
  entityCount: 3
maps: []
grids:
- 1
orphans:
- 1
nullspace: []
tilemap: {}
entities:
- proto: """"
  entities:
  - uid: 1
    mapInit: true
    components:
    - type: MetaData
    - type: Transform
    - type: MapGrid
      chunks: {}
    - type: Broadphase
    - type: Physics
      canCollide: False
    - type: Fixtures
      fixtures: {}
    - type: MapSaveTileMap
- proto: MapDeserializeTest
  entities:
  - uid: 2
    mapInit: true
    components:
    - type: Transform
      parent: 1
    - type: MapDeserializeTest
      foo: 3
";

        private const string Prototype = @"
- type: entity
  id: MapDeserializeTest
  components:
  - type: MapDeserializeTest
    foo: 1
    bar: 2
";

        [Test]
        public async Task TestDataLoadPriority()
        {
            var opts = new ServerIntegrationOptions()
            {
                ExtraPrototypes = Prototype
            };

            var server = StartServer(opts);
            await server.WaitIdleAsync();

            var resourceManager = server.ResolveDependency<IResourceManagerInternal>();
            resourceManager.MountString("/TestMap.yml", MapData);

            var traversal = server.System<SharedGridTraversalSystem>();
            traversal.Enabled = false;
            var mapLoad = server.System<MapLoaderSystem>();

            Entity<MapGridComponent>? grid = default;
            await server.WaitPost(() =>
            {
                server.System<SharedMapSystem>().CreateMap(out var mapId);
                Assert.That(mapLoad.TryLoadGrid(mapId, new ResPath("/TestMap.yml"), out grid));
            });

            var geid = grid!.Value.Owner;

            var entity = server.EntMan.GetComponent<TransformComponent>(geid)._children.Single();
            var c = server.EntMan.GetComponent<MapDeserializeTestComponent>(entity);
            traversal.Enabled = true;

            Assert.That(c.Bar, Is.EqualTo(2));
            Assert.That(c.Foo, Is.EqualTo(3));
            Assert.That(c.Baz, Is.EqualTo(-1));
        }

        [RegisterComponent]
        private sealed partial class MapDeserializeTestComponent : Component
        {
            [DataField("foo")] public int Foo { get; set; } = -1;
            [DataField("bar")] public int Bar { get; set; } = -1;
            [DataField("baz")] public int Baz { get; set; } = -1;
        }
    }
}
