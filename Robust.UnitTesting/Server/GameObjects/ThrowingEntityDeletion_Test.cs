using System.Linq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.UnitTesting.Server.GameObjects
{
    [TestFixture]
    public sealed class ThrowingEntityDeletion_Test
    {
        private ISimulation _sim = default!;

        const string PROTOTYPES = @"
- type: entity
  id: throwInAdd
  components:
  - type: DebugExceptionOnAdd
- type: entity
  id: throwsInInitialize
  components:
  - type: DebugExceptionInitialize
- type: entity
  id: throwsInStartup
  components:
  - type: DebugExceptionStartup
";
        [OneTimeSetUp]
        public void Setup()
        {
            _sim = RobustServerSimulation.NewSimulation()
                .RegisterComponents(f =>
                {
                    f.RegisterClass<DebugExceptionOnAddComponent>();
                    f.RegisterClass<DebugExceptionInitializeComponent>();
                    f.RegisterClass<DebugExceptionStartupComponent>();
                })
                .RegisterEntitySystems(f => f.LoadExtraSystemType<DebugExceptionSystem>())
                .RegisterPrototypes(protoMan => protoMan.LoadString(PROTOTYPES))
                .InitializeInstance();

            _sim.AddMap(1);
        }

        [TestCase("throwInAdd")]
        [TestCase("throwsInInitialize")]
        [TestCase("throwsInStartup")]
        public void Test(string prototypeName)
        {
            var entMan = _sim.Resolve<IEntityManager>();

            Assert.That(() => entMan.SpawnEntity(prototypeName, new MapCoordinates(0, 0, new MapId(1))),
                Throws.TypeOf<EntityCreationException>());

            Assert.That(entMan.GetEntities().Where(p => entMan.GetComponent<MetaDataComponent>(p).EntityPrototype?.ID == prototypeName), Is.Empty);
        }
    }
}
