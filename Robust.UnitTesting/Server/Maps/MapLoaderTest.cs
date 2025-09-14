using System.Linq;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Server.Maps
{
    [TestFixture]
    public sealed class MapLoaderTest : RobustUnitTest
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

        [OneTimeSetUp]
        public void Setup()
        {
            // For some reason RobustUnitTest doesn't discover PVSSystem but this does here so ?
            var syssy = IoCManager.Resolve<IEntitySystemManager>();
            syssy.Shutdown();
            syssy.Initialize();

            var compFactory = IoCManager.Resolve<IComponentFactory>();
            compFactory.RegisterClass<MapDeserializeTestComponent>();
            compFactory.RegisterClass<VisibilityComponent>();
            compFactory.GenerateNetIds();
            IoCManager.Resolve<ISerializationManager>().Initialize();

            var resourceManager = IoCManager.Resolve<IResourceManagerInternal>();
            resourceManager.Initialize(null, hideUserDataDir: false);
            resourceManager.MountString("/TestMap.yml", MapData);
            resourceManager.MountString("/EnginePrototypes/TestMapEntity.yml", Prototype);

            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            protoMan.RegisterKind(typeof(EntityPrototype));

            protoMan.LoadDirectory(new ResourcePath("/EnginePrototypes"));
            protoMan.LoadDirectory(new ResourcePath("/Prototypes"));
            protoMan.ResolveResults();
        }

        [Test]
        public void TestDataLoadPriority()
        {
            // TODO: Fix after serv3
            var map = IoCManager.Resolve<IMapManager>();

            var entMan = IoCManager.Resolve<IEntityManager>();

            var mapId = map.CreateMap();
            // Yay test bullshit
            var mapUid = map.GetMapEntityId(mapId);
            entMan.EnsureComponent<PhysicsMapComponent>(mapUid);
            entMan.EnsureComponent<BroadphaseComponent>(mapUid);

            var mapLoad = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
            var geid = mapLoad.LoadGrid(mapId, "/TestMap.yml");

            Assert.That(geid, NUnit.Framework.Is.Not.Null);

            var entity = entMan.GetComponent<TransformComponent>(geid!.Value).Children.Single().Owner;
            var c = entMan.GetComponent<MapDeserializeTestComponent>(entity);

            Assert.That(c.Bar, Is.EqualTo(2));
            Assert.That(c.Foo, Is.EqualTo(3));
            Assert.That(c.Baz, Is.EqualTo(-1));
        }

        [DataDefinition]
        private sealed class MapDeserializeTestComponent : Component
        {
            [DataField("foo")] public int Foo { get; set; } = -1;
            [DataField("bar")] public int Bar { get; set; } = -1;
            [DataField("baz")] public int Baz { get; set; } = -1;
        }
    }
}
