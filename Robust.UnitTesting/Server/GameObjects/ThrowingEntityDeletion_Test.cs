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
    public class ThrowingEntityDeletion_Test : RobustUnitTest
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

            _componentFactory.Register<ThrowsInAddComponent>();
            _componentFactory.Register<ThrowsInInitializeComponent>();
            _componentFactory.Register<ThrowsInStartupComponent>();
            
            EntityManager = IoCManager.Resolve<IServerEntityManager>();
            MapManager = IoCManager.Resolve<IMapManager>();

            MapManager.CreateNewMapEntity(MapId.Nullspace);

            IoCManager.Resolve<ISerializationManager>().Initialize();
            var manager = IoCManager.Resolve<IPrototypeManager>();
            manager.LoadFromStream(new StringReader(PROTOTYPES));
            manager.Resync();

            //NOTE: The grids have not moved, so we can assert worldpos == localpos for the test
        }

        [Test]
        public void Test([Values("throwInAdd", "throwsInInitialize", "throwsInStartup")]
            string prototypeName)
        {
            Assert.That(() => EntityManager.SpawnEntity(prototypeName, MapCoordinates.Nullspace),
                Throws.TypeOf<EntityCreationException>());

            Assert.That(EntityManager.GetEntities().Where(p => p.Prototype?.ID == prototypeName), Is.Empty);
        }

        private sealed class ThrowsInAddComponent : Component
        {
            public override string Name => "ThrowsInAdd";

            public override void OnAdd() => throw new NotSupportedException();
        }

        private sealed class ThrowsInInitializeComponent : Component
        {
            public override string Name => "ThrowsInInitialize";

            public override void Initialize() => throw new NotSupportedException();
        }

        private sealed class ThrowsInStartupComponent : Component
        {
            public override string Name => "ThrowsInStartup";

            protected override void Startup() => throw new NotSupportedException();
        }
    }
}
