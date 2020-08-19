using Moq;
using Robust.Server.GameObjects;
using Robust.Server.Timing;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Localization.Macros;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Server
{
    internal interface ISimulationFactory
    {
        ISimulationFactory RegisterComponents(CompRegistrationDelegate factory);
        ISimulationFactory RegisterDependencies(DiContainerDelegate factory);
        ISimulationFactory RegisterEntitySystems(EntitySystemRegistrationDelegate factory);
        ISimulationFactory RegisterPrototypes(PrototypeRegistrationDelegate factory);
        ISimulation InitializeInstance();
    }

    internal interface ISimulation
    {
        IDependencyCollection Collection { get; }

        /// <summary>
        /// Resolves a dependency directly out of IoC collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Resolve<T>();
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

            // System
            container.Register<ILogManager, LogManager>();
            container.Register<IConfigurationManager, ConfigurationManager>();
            container.Register<IDynamicTypeFactory, DynamicTypeFactory>();
            container.Register<IDynamicTypeFactoryInternal, DynamicTypeFactory>();
            container.Register<ILocalizationManager, LocalizationManager>();
            container.Register<IModLoader, TestingModLoader>();
            container.Register<IModLoaderInternal, TestingModLoader>();
            container.RegisterInstance<IReflectionManager>(new Mock<IReflectionManager>().Object);
            container.RegisterInstance<IResourceManager>(new Mock<IResourceManager>().Object);
            container.RegisterInstance<IGameTiming>(new Mock<IGameTiming>().Object);

            // Simulation
            container.Register<IServerEntityManager, ServerEntityManager>();
            container.Register<IEntityManager, ServerEntityManager>();
            container.Register<IComponentManager, ComponentManager>();
            container.Register<IMapManager, MapManager>();
            container.Register<IPrototypeManager, PrototypeManager>();
            container.Register<IComponentFactory, ComponentFactory>();
            container.Register<IEntitySystemManager, EntitySystemManager>();
            container.Register<IPhysicsManager, PhysicsManager>();
            container.RegisterInstance<IPauseManager>(new Mock<IPauseManager>().Object);
            
            // Networking
            container.RegisterInstance<IEntityNetworkManager>(new Mock<IEntityNetworkManager>().Object);
            container.RegisterInstance<ITextMacroFactory>( new Mock<ITextMacroFactory>().Object);
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
