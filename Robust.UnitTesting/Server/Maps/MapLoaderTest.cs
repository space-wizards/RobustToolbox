using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Resource;
using Moq;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Server.Physics;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Server.Maps
{
    [TestFixture]
    public class MapLoaderTest : RobustIntegrationTest
    {
        private const string MapData = @"
meta:
  format: 2
  name: DemoStation
  author: Space-Wizards
  postmapinit: false
grids:
- settings:
    chunksize: 16
    tilesize: 1
    snapsize: 1
  chunks: []
tilemap: {}
entities:
- uid: 0
  components:
  - parent: null
    type: Transform
  - index: 0
    type: MapGrid
- uid: 1
  type: MapDeserializeTest
  components:
  - type: MapDeserializeTest
    foo: 3
  - parent: 0
    type: Transform
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
            var sim = StartServer();

            await sim.WaitIdleAsync();

            var compFactory = sim.ResolveDependency<IComponentFactory>();
            compFactory.RegisterClass<MapDeserializeTestComponent>();

            sim.ResolveDependency<IPrototypeManager>().LoadString(Prototype);

            var resourceManager = sim.ResolveDependency<IResourceManagerInternal>();
            resourceManager.Initialize(null);
            resourceManager.MountString("/TestMap.yml", MapData);
            resourceManager.MountString("/Prototypes/TestMapEntity.yml", Prototype);

            sim.ResolveDependency<IPrototypeManager>().LoadDirectory(new ResourcePath("/Prototypes"));

            // TODO: Fix after serv3
            var entMan = sim.ResolveDependency<IEntityManager>();
            var mapMan = sim.ResolveDependency<IMapManager>();

            var mapid = mapMan.CreateMap();
            var mapLoad = sim.ResolveDependency<IMapLoader>();
            var grid = mapLoad.LoadBlueprint(mapid, "/TestMap.yml");

            Assert.That(grid, NUnit.Framework.Is.Not.Null);

            var entity = entMan.GetEntity(grid!.GridEntityId).Transform.Children.Single().Owner;
            var c = entity.GetComponent<MapDeserializeTestComponent>();

            Assert.That(c.Bar, Is.EqualTo(2));
            Assert.That(c.Foo, Is.EqualTo(3));
            Assert.That(c.Baz, Is.EqualTo(-1));
        }

        [DataDefinition]
        private sealed class MapDeserializeTestComponent : Component
        {
            public override string Name => "MapDeserializeTest";

            [DataField("foo")] public int Foo { get; set; } = -1;
            [DataField("bar")] public int Bar { get; set; } = -1;
            [DataField("baz")] public int Baz { get; set; } = -1;
        }
    }
}
