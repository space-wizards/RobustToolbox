using System.Linq;
using NUnit.Framework;
using Robust.Server.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Server.Maps
{
    [TestFixture]
    public class MapLoaderTest : RobustUnitTest
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
  - shapes:
    - !type:PhysShapeGrid
      grid: 0
    type: Physics
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


        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<IComponentFactory>().Register<MapDeserializeTestComponent>();

            IoCManager.Resolve<IComponentManager>().Initialize();

            var resourceManager = IoCManager.Resolve<IResourceManagerInternal>();
            resourceManager.Initialize(null);
            resourceManager.MountString("/TestMap.yml", MapData);
            resourceManager.MountString("/Prototypes/TestMapEntity.yml", Prototype);

            IoCManager.Resolve<IPrototypeManager>().LoadDirectory(new ResourcePath("/Prototypes"));

            var map = IoCManager.Resolve<IMapManager>();
            map.Initialize();
            map.Startup();
        }

        [Test]
        public void TestDataLoadPriority()
        {
            var map = IoCManager.Resolve<IMapManager>();

            var entMan = IoCManager.Resolve<IEntityManager>();

            var mapId = map.CreateMap();
            var mapLoad = IoCManager.Resolve<IMapLoader>();
            var grid = mapLoad.LoadBlueprint(mapId, "/TestMap.yml");

            Assert.That(grid, NUnit.Framework.Is.Not.Null);

            var entity = entMan.GetEntity(grid!.GridEntityId).Transform.Children.Single().Owner;
            var c = entity.GetComponent<MapDeserializeTestComponent>();

            Assert.That(c.Bar, Is.EqualTo(2));
            Assert.That(c.Foo, Is.EqualTo(3));
            Assert.That(c.Baz, Is.EqualTo(-1));
        }

        private sealed class MapDeserializeTestComponent : Component
        {
            public override string Name => "MapDeserializeTest";

            [DataField("foo")] public int Foo { get; set; } = -1;
            [DataField("bar")] public int Bar { get; set; } = -1;
            [DataField("baz")] public int Baz { get; set; } = -1;
        }
    }
}
