using JetBrains.Annotations;
using Moq;
using Robust.Server.GameObjects;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Server
{
    [PublicAPI]
    internal interface ISimulationFactory
    {
        ISimulationFactory RegisterComponents(CompRegistrationDelegate factory);
        ISimulationFactory RegisterDependencies(DiContainerDelegate factory);
        ISimulationFactory RegisterEntitySystems(EntitySystemRegistrationDelegate factory);
        ISimulationFactory RegisterPrototypes(PrototypeRegistrationDelegate factory);
        ISimulation InitializeInstance();
    }

    [PublicAPI]
    internal interface ISimulation
    {
        IDependencyCollection Collection { get; }

        /// <summary>
        /// Resolves a dependency directly out of IoC collection.
        /// </summary>
        T Resolve<T>();

        /// <summary>
        /// Adds a new map directly to the map manager.
        /// </summary>
        EntityUid AddMap(int mapId);
        EntityUid AddMap(MapId mapId);
        IEntity SpawnEntity(string? protoId, EntityCoordinates coordinates);
        IEntity SpawnEntity(string? protoId, MapCoordinates coordinates);
    }

    internal delegate void DiContainerDelegate(IDependencyCollection diContainer);

    internal delegate void CompRegistrationDelegate(IComponentFactory factory);

    internal delegate void EntitySystemRegistrationDelegate(IEntitySystemManager systemMan);

    internal delegate void PrototypeRegistrationDelegate(IPrototypeManager protoMan);

    internal class RobustServerSimulation : ISimulation, ISimulationFactory
    {
        private DiContainerDelegate? _diFactory;
        private CompRegistrationDelegate? _regDelegate;
        private EntitySystemRegistrationDelegate? _systemDelegate;
        private PrototypeRegistrationDelegate? _protoDelegate;

        public IDependencyCollection Collection { get; private set; } = default!;

        public T Resolve<T>()
        {
            return Collection.Resolve<T>();
        }

        public EntityUid AddMap(int mapId)
        {
            var mapMan = Collection.Resolve<IMapManager>();
            mapMan.CreateMap(new MapId(mapId));
            return mapMan.GetMapEntityId(new MapId(mapId));
        }

        public EntityUid AddMap(MapId mapId)
        {
            var mapMan = Collection.Resolve<IMapManager>();
            mapMan.CreateMap(mapId);
            return mapMan.GetMapEntityId(mapId);
        }

        public IEntity SpawnEntity(string? protoId, EntityCoordinates coordinates)
        {
            var entMan = Collection.Resolve<IEntityManager>();
            return entMan.SpawnEntity(protoId, coordinates);
        }

        public IEntity SpawnEntity(string? protoId, MapCoordinates coordinates)
        {
            var entMan = Collection.Resolve<IEntityManager>();
            return entMan.SpawnEntity(protoId, coordinates);
        }

        private RobustServerSimulation() { }

        public ISimulationFactory RegisterDependencies(DiContainerDelegate factory)
        {
            _diFactory += factory;
            return this;
        }

        public ISimulationFactory RegisterComponents(CompRegistrationDelegate factory)
        {
            _regDelegate += factory;
            return this;
        }

        public ISimulationFactory RegisterEntitySystems(EntitySystemRegistrationDelegate factory)
        {
            _systemDelegate += factory;
            return this;
        }

        public ISimulationFactory RegisterPrototypes(PrototypeRegistrationDelegate factory)
        {
            _protoDelegate += factory;
            return this;
        }

        public ISimulation InitializeInstance()
        {
            var container = new DependencyCollection();
            Collection = container;

            IoCManager.InitThread(container, true);

            //TODO: This is a long term project that should eventually have parity with the actual server/client/SP IoC registration.
            // The goal is to be able to pull out all networking and frontend dependencies, and only have a core simulation running.
            // This does NOT replace the full RobustIntegrationTest, or regular unit testing. This simulation sits in the middle
            // and allows you to run integration testing only on the simulation.

            //Tier 1: System
            container.Register<ILogManager, LogManager>();
            container.Register<IRuntimeLog, RuntimeLog>();
            container.Register<IConfigurationManager, ConfigurationManager>();
            container.Register<IDynamicTypeFactory, DynamicTypeFactory>();
            container.Register<IDynamicTypeFactoryInternal, DynamicTypeFactory>();
            container.Register<ILocalizationManager, LocalizationManager>();
            container.Register<IModLoader, TestingModLoader>();
            container.Register<IModLoaderInternal, TestingModLoader>();
            container.RegisterInstance<ITaskManager>(new Mock<ITaskManager>().Object);
            container.RegisterInstance<IReflectionManager>(new Mock<IReflectionManager>().Object); // tests should not be searching for types
            container.RegisterInstance<IRobustSerializer>(new Mock<IRobustSerializer>().Object);
            container.RegisterInstance<IResourceManager>(new Mock<IResourceManager>().Object); // no disk access for tests
            container.RegisterInstance<IGameTiming>(new Mock<IGameTiming>().Object); // TODO: get timing working similar to RobustIntegrationTest

            //Tier 2: Simulation
            container.Register<IServerEntityManager, ServerEntityManager>();
            container.Register<IEntityManager, ServerEntityManager>();
            container.Register<IComponentManager, ComponentManager>();
            container.Register<IMapManager, MapManager>();
            container.Register<IPrototypeManager, PrototypeManager>();
            container.Register<IComponentFactory, ComponentFactory>();
            container.Register<IComponentDependencyManager, ComponentDependencyManager>();
            container.Register<IEntitySystemManager, EntitySystemManager>();
            container.Register<IPhysicsManager, PhysicsManager>();
            container.RegisterInstance<IPauseManager>(new Mock<IPauseManager>().Object); // TODO: get timing working similar to RobustIntegrationTest

            //Tier 3: Networking
            //TODO: Try to remove these
            container.RegisterInstance<IEntityNetworkManager>(new Mock<IEntityNetworkManager>().Object);
            container.RegisterInstance<INetManager>(new Mock<INetManager>().Object);

            _diFactory?.Invoke(container);
            container.BuildGraph();

            var logMan = container.Resolve<ILogManager>();
            logMan.RootSawmill.AddHandler(new TestLogHandler("SIM"));

            var compFactory = container.Resolve<IComponentFactory>();

            compFactory.Register<MetaDataComponent>();
            compFactory.RegisterReference<MetaDataComponent, IMetaDataComponent>();

            compFactory.Register<TransformComponent>();
            compFactory.RegisterReference<TransformComponent, ITransformComponent>();

            compFactory.Register<MapComponent>();
            compFactory.RegisterReference<MapComponent, IMapComponent>();

            compFactory.Register<MapGridComponent>();
            compFactory.RegisterReference<MapGridComponent, IMapGridComponent>();

            compFactory.Register<PhysicsComponent>();
            compFactory.RegisterReference<PhysicsComponent, IPhysBody>();

            _regDelegate?.Invoke(compFactory);

            var entityMan = container.Resolve<IEntityManager>();
            entityMan.Initialize();
            _systemDelegate?.Invoke(container.Resolve<IEntitySystemManager>());
            entityMan.Startup();

            var mapManager = container.Resolve<IMapManager>();
            mapManager.Initialize();
            mapManager.Startup();

            var protoMan = container.Resolve<IPrototypeManager>();
            protoMan.RegisterType(typeof(EntityPrototype));
            _protoDelegate?.Invoke(protoMan);
            protoMan.Resync();

            return this;
        }

        public static ISimulationFactory NewSimulation()
        {
            return new RobustServerSimulation();
        }
    }
}
