using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Moq;
using Robust.Client.HWId;
using Robust.Server;
using Robust.Server.Configuration;
using Robust.Server.Console;
using Robust.Server.Containers;
using Robust.Server.Debugging;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Localization;
using Robust.Server.Physics;
using Robust.Server.Player;
using Robust.Server.Prototypes;
using Robust.Server.Reflection;
using Robust.Server.Replays;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Profiling;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Reflection;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Threading;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Server
{
    [PublicAPI]
    public interface ISimulationFactory
    {
        ISimulationFactory RegisterComponents(CompRegistrationDelegate factory);
        ISimulationFactory RegisterDependencies(DiContainerDelegate factory);
        ISimulationFactory RegisterEntitySystems(EntitySystemRegistrationDelegate factory);
        ISimulationFactory RegisterPrototypes(PrototypeRegistrationDelegate factory);
        ISimulation InitializeInstance();
    }

    [PublicAPI]
    public interface ISimulation
    {
        IDependencyCollection Collection { get; }

        /// <summary>
        /// Resolves a dependency directly out of IoC collection.
        /// </summary>
        T Resolve<T>();

        /// <summary>
        /// Adds a new map directly to the map manager.
        /// </summary>
        (EntityUid Uid, MapId MapId) CreateMap();
        EntityUid SpawnEntity(string? protoId, EntityCoordinates coordinates);
        EntityUid SpawnEntity(string? protoId, MapCoordinates coordinates);
    }

    /// <summary>
    /// Helper methods for working with <see cref="ISimulation"/>.
    /// </summary>
    internal static class SimulationExtensions
    {
        public static T System<T>(this ISimulation simulation) where T : IEntitySystem
        {
            return simulation.Resolve<IEntitySystemManager>().GetEntitySystem<T>();
        }

        public static bool HasComp<T>(this ISimulation simulation, EntityUid entity) where T : IComponent
        {
            return simulation.Resolve<IEntityManager>().HasComponent<T>(entity);
        }

        public static T Comp<T>(this ISimulation simulation, EntityUid entity) where T : IComponent
        {
            return simulation.Resolve<IEntityManager>().GetComponent<T>(entity);
        }

        public static TransformComponent Transform(this ISimulation simulation, EntityUid entity)
        {
            return simulation.Comp<TransformComponent>(entity);
        }
    }

    public delegate void DiContainerDelegate(IDependencyCollection diContainer);

    public delegate void CompRegistrationDelegate(IComponentFactory factory);

    public delegate void EntitySystemRegistrationDelegate(IEntitySystemManager systemMan);

    public delegate void PrototypeRegistrationDelegate(IPrototypeManager protoMan);

    public sealed class RobustServerSimulation : ISimulation, ISimulationFactory
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

        public (EntityUid Uid, MapId MapId) CreateMap()
        {
            var uid = Collection.Resolve<IEntityManager>().System<SharedMapSystem>().CreateMap(out var mapId);
            return (uid, mapId);
        }

        public EntityUid SpawnEntity(string? protoId, EntityCoordinates coordinates)
        {
            var entMan = Collection.Resolve<IEntityManager>();
            return entMan.SpawnEntity(protoId, coordinates);
        }

        public EntityUid SpawnEntity(string? protoId, MapCoordinates coordinates)
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
            container.Register<IConfigurationManager, ServerNetConfigurationManager>();
            container.Register<INetConfigurationManager, ServerNetConfigurationManager>();
            container.Register<IConfigurationManagerInternal, ServerNetConfigurationManager>();
            container.Register<IServerNetConfigurationManager, ServerNetConfigurationManager>();
            container.Register<INetConfigurationManagerInternal, ServerNetConfigurationManager>();
            container.Register<IDynamicTypeFactory, DynamicTypeFactory>();
            container.Register<IDynamicTypeFactoryInternal, DynamicTypeFactory>();
            container.Register<ILocalizationManager, ServerLocalizationManager>();
            container.Register<IModLoader, TestingModLoader>();
            container.Register<IModLoaderInternal, TestingModLoader>();
            container.Register<ProfManager, ProfManager>();
            container.RegisterInstance<ITaskManager>(new Mock<ITaskManager>().Object);
            container.Register<HttpClientHolder>();
            container.Register<IHttpClientHolder, HttpClientHolder>();
            container.Register<IHWId, DummyHWId>();

            var realReflection = new ServerReflectionManager();
            realReflection.LoadAssemblies(new List<Assembly>(2)
            {
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"),
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Server"),
            });

            var reflectionManager = new Mock<IReflectionManager>();
            reflectionManager
                .Setup(x => x.FindTypesWithAttribute<MeansDataDefinitionAttribute>())
                .Returns(() => new[]
                {
                    typeof(DataDefinitionAttribute)
                });

            reflectionManager
                .Setup(x => x.FindTypesWithAttribute(typeof(DataDefinitionAttribute)))
                .Returns(() => new[]
                {
                    typeof(EntityPrototype),
                    typeof(TransformComponent),
                    typeof(MetaDataComponent)
                });

            reflectionManager
                .Setup(x => x.FindTypesWithAttribute<TypeSerializerAttribute>())
                .Returns(() => realReflection.FindTypesWithAttribute<TypeSerializerAttribute>());

            reflectionManager
                .Setup(x => x.FindAllTypes())
                .Returns(() => realReflection.FindAllTypes());

            container.RegisterInstance<IBaseServerInternal>(new Mock<IBaseServerInternal>().Object);
            container.RegisterInstance<IReflectionManager>(reflectionManager.Object); // tests should not be searching for types
            container.RegisterInstance<IRobustSerializer>(new Mock<IRobustSerializer>().Object);
            container.RegisterInstance<IResourceManager>(new Mock<IResourceManager>().Object); // no disk access for tests
            container.RegisterInstance<IGameTiming>(new Mock<IGameTiming>().Object); // TODO: get timing working similar to RobustIntegrationTest

            //Tier 2: Simulation
            container.RegisterInstance<IConsoleHost>(new Mock<IConsoleHost>().Object); //Console is technically a frontend, we want to run headless
            container.Register<IEntityManager, ServerEntityManager>();
            container.Register<IServerEntityNetworkManager, ServerEntityManager>();
            container.Register<EntityManager, ServerEntityManager>();
            container.Register<IMapManager, NetworkedMapManager>();
            container.Register<INetworkedMapManager, NetworkedMapManager>();
            container.Register<IMapManagerInternal, NetworkedMapManager>();
            container.Register<ISerializationManager, SerializationManager>();
            container.Register<IRobustRandom, RobustRandom>();
            container.Register<IPrototypeManager, ServerPrototypeManager>();
            container.Register<IComponentFactory, ComponentFactory>();
            container.Register<IEntitySystemManager, EntitySystemManager>();
            container.Register<IManifoldManager, CollisionManager>();
            container.Register<INetManager, NetManager>();
            container.Register<IAuthManager, AuthManager>();
            container.Register<ITileDefinitionManager, TileDefinitionManager>();
            container.Register<IParallelManager, TestingParallelManager>();
            container.Register<IParallelManagerInternal, TestingParallelManager>();
            // Needed for grid fixture debugging.
            container.Register<IConGroupController, ConGroupController>();
            container.Register<EntityConsoleHost>();

            // I just wanted to load pvs system
            container.Register<IServerEntityManager, ServerEntityManager>();
            container.Register<IServerNetManager, NetManager>();
            // god help you if you actually need to test pvs functions
            container.RegisterInstance<IPlayerManager>(new Mock<IPlayerManager>().Object);
            container.RegisterInstance<ISharedPlayerManager>(new Mock<ISharedPlayerManager>().Object);
            container.RegisterInstance<IServerGameStateManager>(new Mock<IServerGameStateManager>().Object);
            container.RegisterInstance<IReplayRecordingManager>(new Mock<IReplayRecordingManager>().Object);
            container.RegisterInstance<IServerReplayRecordingManager>(new Mock<IServerReplayRecordingManager>().Object);

            _diFactory?.Invoke(container);
            container.BuildGraph();

            // Because of CVarDef, we have to load every one through reflection
            // just in case a system needs one of them.
            var configMan = container.Resolve<IConfigurationManagerInternal>();
            configMan.Initialize(true);
            configMan.LoadCVarsFromAssembly(typeof(Program).Assembly); // Server
            configMan.LoadCVarsFromAssembly(typeof(ProgramShared).Assembly); // Shared
            configMan.LoadCVarsFromAssembly(typeof(RobustServerSimulation).Assembly); // Tests

            var logMan = container.Resolve<ILogManager>();
            logMan.RootSawmill.AddHandler(new TestLogHandler(configMan, "SIM"));

            var compFactory = container.Resolve<IComponentFactory>();

            // if only we had some sort of attribute for automatically registering components.
            compFactory.RegisterClass<MetaDataComponent>();
            compFactory.RegisterClass<TransformComponent>();
            compFactory.RegisterClass<MapGridComponent>();
            compFactory.RegisterClass<MapComponent>();
            compFactory.RegisterClass<MapLightComponent>();
            compFactory.RegisterClass<PhysicsComponent>();
            compFactory.RegisterClass<JointComponent>();
            compFactory.RegisterClass<EyeComponent>();
            compFactory.RegisterClass<GridTreeComponent>();
            compFactory.RegisterClass<MovedGridsComponent>();
            compFactory.RegisterClass<JointRelayTargetComponent>();
            compFactory.RegisterClass<BroadphaseComponent>();
            compFactory.RegisterClass<ContainerManagerComponent>();
            compFactory.RegisterClass<PhysicsMapComponent>();
            compFactory.RegisterClass<FixturesComponent>();
            compFactory.RegisterClass<CollisionWakeComponent>();
            compFactory.RegisterClass<OccluderComponent>();
            compFactory.RegisterClass<OccluderTreeComponent>();
            compFactory.RegisterClass<Gravity2DComponent>();
            compFactory.RegisterClass<CollideOnAnchorComponent>();
            compFactory.RegisterClass<ActorComponent>();

            _regDelegate?.Invoke(compFactory);

            compFactory.GenerateNetIds();

            var entityMan = container.Resolve<IEntityManager>();
            entityMan.Initialize();

            var entitySystemMan = container.Resolve<IEntitySystemManager>();

            entitySystemMan.LoadExtraSystemType<PhysicsSystem>();
            entitySystemMan.LoadExtraSystemType<Gravity2DController>();
            entitySystemMan.LoadExtraSystemType<SharedGridTraversalSystem>();
            entitySystemMan.LoadExtraSystemType<ContainerSystem>();
            entitySystemMan.LoadExtraSystemType<JointSystem>();
            entitySystemMan.LoadExtraSystemType<MapSystem>();
            entitySystemMan.LoadExtraSystemType<DebugPhysicsSystem>();
            entitySystemMan.LoadExtraSystemType<DebugRayDrawingSystem>();
            entitySystemMan.LoadExtraSystemType<BroadPhaseSystem>();
            entitySystemMan.LoadExtraSystemType<CollisionWakeSystem>();
            entitySystemMan.LoadExtraSystemType<FixtureSystem>();
            entitySystemMan.LoadExtraSystemType<GridFixtureSystem>();
            entitySystemMan.LoadExtraSystemType<TransformSystem>();
            entitySystemMan.LoadExtraSystemType<EntityLookupSystem>();
            entitySystemMan.LoadExtraSystemType<ServerMetaDataSystem>();
            entitySystemMan.LoadExtraSystemType<PvsSystem>();
            entitySystemMan.LoadExtraSystemType<InputSystem>();
            entitySystemMan.LoadExtraSystemType<PvsOverrideSystem>();

            _systemDelegate?.Invoke(entitySystemMan);

            var mapManager = container.Resolve<IMapManager>();
            mapManager.Initialize();

            entityMan.Startup();
            mapManager.Startup();

            container.Resolve<INetManager>().Initialize(true);
            container.Resolve<ISerializationManager>().Initialize();

            var protoMan = container.Resolve<IPrototypeManager>();
            protoMan.Initialize();
            protoMan.RegisterKind(typeof(EntityPrototype), typeof(EntityCategoryPrototype));
            _protoDelegate?.Invoke(protoMan);

            // This just exists to set protoMan._hasEverBeenReloaded to True
            // The code is perfect.
            protoMan.LoadString("");

            protoMan.ResolveResults();

            return this;
        }

        public static ISimulationFactory NewSimulation()
        {
            return new RobustServerSimulation();
        }
    }
}
