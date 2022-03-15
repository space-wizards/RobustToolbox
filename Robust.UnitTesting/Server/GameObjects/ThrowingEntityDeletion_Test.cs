using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.UnitTesting.Server.GameObjects
{
    [TestFixture]
    public sealed class ThrowingEntityDeletion_Test : RobustUnitTest
    {
        private IServerEntityManager EntityManager = default!;
        private IComponentFactory _componentFactory = default!;
        private IMapManager MapManager = default!;

        const string PROTOTYPES = @"
- type: entity
  id: throwInAdd
  components:
  - type: ThrowsInAdd
- type: entity
  id: throwsInInitialize
  components:
  - type: ThrowsInInitialize
- type: entity
  id: throwsInStartup
  components:
  - type: ThrowsInStartup
";

        [OneTimeSetUp]
        public void Setup()
        {
            _componentFactory = IoCManager.Resolve<IComponentFactory>();

            _componentFactory.RegisterClass<ThrowsInAddComponent>();
            _componentFactory.RegisterClass<ThrowsInInitializeComponent>();
            _componentFactory.RegisterClass<ThrowsInStartupComponent>();
            _componentFactory.GenerateNetIds();

            EntityManager = IoCManager.Resolve<IServerEntityManager>();
            MapManager = IoCManager.Resolve<IMapManager>();

            MapManager.CreateNewMapEntity(MapId.Nullspace);

            IoCManager.Resolve<ISerializationManager>().Initialize();
            var manager = IoCManager.Resolve<IPrototypeManager>();
            manager.RegisterType(typeof(EntityPrototype));
            manager.LoadFromStream(new StringReader(PROTOTYPES));
            manager.ResolveResults();

            //NOTE: The grids have not moved, so we can assert worldpos == localpos for the test
        }

        [TestCase("throwInAdd")]
        [TestCase("throwsInInitialize")]
        [TestCase("throwsInStartup")]
        public void Test(string prototypeName)
        {
            Assert.That(() => EntityManager.SpawnEntity(prototypeName, MapCoordinates.Nullspace),
                Throws.TypeOf<EntityCreationException>());

            Assert.That(EntityManager.GetEntities().Where(p => IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(p).EntityPrototype?.ID == prototypeName), Is.Empty);
        }

        private sealed class ThrowsInAddComponent : Component
        {
            protected override void OnAdd() => throw new NotSupportedException();
        }

        private sealed class ThrowsInInitializeComponent : Component
        {
            protected override void Initialize() => throw new NotSupportedException();
        }

        private sealed class ThrowsInStartupComponent : Component
        {
            protected override void Startup() => throw new NotSupportedException();
        }
    }
}
