using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Server.Debugging;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Physics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Threading;
using Robust.Shared.Utility;
using InputSystem = Robust.Server.GameObjects.InputSystem;
using MapSystem = Robust.Server.GameObjects.MapSystem;
using PointLightComponent = Robust.Client.GameObjects.PointLightComponent;

namespace Robust.UnitTesting
{
    public enum UnitTestProject : byte
    {
        Server,
        Client
    }

    [Parallelizable]
    public abstract partial class RobustUnitTest
    {
        protected virtual Type[]? ExtraComponents => null;
        private static Type[] _components = new []
            {
                typeof(EyeComponent),
                typeof(MapComponent),
                typeof(MapGridComponent),
                typeof(ContainerManagerComponent),
                typeof(MetaDataComponent),
                typeof(TransformComponent),
                typeof(PhysicsComponent),
                typeof(PhysicsMapComponent),
                typeof(BroadphaseComponent),
                typeof(FixturesComponent),
                typeof(JointComponent),
                typeof(GridTreeComponent),
                typeof(MovedGridsComponent),
                typeof(JointRelayTargetComponent),
                typeof(OccluderComponent),
                typeof(OccluderTreeComponent),
                typeof(SpriteTreeComponent),
                typeof(LightTreeComponent),
                typeof(CollisionWakeComponent),
                typeof(CollideOnAnchorComponent),
                typeof(Gravity2DComponent),
                typeof(ActorComponent)
            };

        public virtual UnitTestProject Project => UnitTestProject.Server;

        [OneTimeSetUp]
        public void BaseSetup()
        {
            // Clear state across tests.
            var deps = IoCManager.InitThread();
            deps.Clear();

            RegisterIoC();

            var assemblies = new List<Assembly>(4);
            switch (Project)
            {
                case UnitTestProject.Client:
                    assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("Robust.Client"));
                    break;
                case UnitTestProject.Server:
                    assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("Robust.Server"));
                    break;
                default:
                    throw new NotSupportedException($"Unknown testing project: {Project}");
            }

            assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"));
            assemblies.Add(Assembly.GetExecutingAssembly());

            var configurationManager = deps.Resolve<IConfigurationManagerInternal>();

            configurationManager.Initialize(Project == UnitTestProject.Server);
            deps.Resolve<IReflectionManager>().Initialize();

            foreach (var assembly in assemblies)
            {
                configurationManager.LoadCVarsFromAssembly(assembly);
            }

            var contentAssemblies = GetContentAssemblies();

            foreach (var assembly in contentAssemblies)
            {
                configurationManager.LoadCVarsFromAssembly(assembly);
            }

            configurationManager.LoadCVarsFromAssembly(typeof(RobustUnitTest).Assembly);

            var systems = deps.Resolve<IEntitySystemManager>();
            // Required systems
            systems.LoadExtraSystemType<EntityLookupSystem>();

            // uhhh so maybe these are the wrong system for the client, but I CBF adding sprite system and all the rest,
            // and it was like this when I found it.

            systems.LoadExtraSystemType<SharedGridTraversalSystem>();
            systems.LoadExtraSystemType<FixtureSystem>();
            systems.LoadExtraSystemType<Gravity2DController>();
            systems.LoadExtraSystemType<CollisionWakeSystem>();

            if (Project == UnitTestProject.Client)
            {
                systems.LoadExtraSystemType<ClientMetaDataSystem>();
                systems.LoadExtraSystemType<ContainerSystem>();
                systems.LoadExtraSystemType<Robust.Client.GameObjects.TransformSystem>();
                systems.LoadExtraSystemType<Robust.Client.Physics.BroadPhaseSystem>();
                systems.LoadExtraSystemType<Robust.Client.Physics.JointSystem>();
                systems.LoadExtraSystemType<Robust.Client.Physics.PhysicsSystem>();
                systems.LoadExtraSystemType<Robust.Client.Debugging.DebugRayDrawingSystem>();
                systems.LoadExtraSystemType<PrototypeReloadSystem>();
                systems.LoadExtraSystemType<Robust.Client.Debugging.DebugPhysicsSystem>();
                systems.LoadExtraSystemType<Robust.Client.GameObjects.MapSystem>();
                systems.LoadExtraSystemType<Robust.Client.GameObjects.PointLightSystem>();
                systems.LoadExtraSystemType<LightTreeSystem>();
                systems.LoadExtraSystemType<RecursiveMoveSystem>();
                systems.LoadExtraSystemType<SpriteSystem>();
                systems.LoadExtraSystemType<SpriteTreeSystem>();
                systems.LoadExtraSystemType<GridChunkBoundsDebugSystem>();
            }
            else
            {
                systems.LoadExtraSystemType<ServerMetaDataSystem>();
                systems.LoadExtraSystemType<PvsSystem>();
                systems.LoadExtraSystemType<Robust.Server.Containers.ContainerSystem>();
                systems.LoadExtraSystemType<Robust.Server.GameObjects.TransformSystem>();
                systems.LoadExtraSystemType<BroadPhaseSystem>();
                systems.LoadExtraSystemType<JointSystem>();
                systems.LoadExtraSystemType<PhysicsSystem>();
                systems.LoadExtraSystemType<DebugRayDrawingSystem>();
                systems.LoadExtraSystemType<PrototypeReloadSystem>();
                systems.LoadExtraSystemType<DebugPhysicsSystem>();
                systems.LoadExtraSystemType<InputSystem>();
                systems.LoadExtraSystemType<PvsOverrideSystem>();
                systems.LoadExtraSystemType<MapSystem>();
            }

            var entMan = deps.Resolve<IEntityManager>();
            var mapMan = deps.Resolve<IMapManager>();

            // Avoid discovering EntityCommands since they may depend on systems
            // that aren't available in a unit test context.
            deps.Resolve<EntityConsoleHost>().DiscoverCommands = false;

            // Required components for the engine to work
            // Why are we still here? Just to suffer? Why can't we just use [RegisterComponent] magic?
            // TODO End Suffering.
            // suffering has been alleviated, but still present
            var compFactory = deps.Resolve<IComponentFactory>();
            compFactory.RegisterTypes(_components);
            if (ExtraComponents != null)
                compFactory.RegisterTypes(ExtraComponents);

            compFactory.RegisterClass<MapSaveTileMapComponent>();
            compFactory.RegisterClass<YamlUidComponent>();

            if (Project != UnitTestProject.Server)
            {
                compFactory.RegisterClass<PointLightComponent>();
                compFactory.RegisterClass<SpriteComponent>();
            }

            deps.Resolve<IParallelManagerInternal>().Initialize();

            // So by default EntityManager does its own EntitySystemManager initialize during Startup.
            // We want to bypass this and load our own systems hence we will manually initialize it here.
            entMan.Initialize();
            // RobustUnitTest is complete hot garbage.
            // This makes EventTables ignore *all* the screwed up component abuse it causes.
            entMan.EventBus.OnlyCallOnRobustUnitTestISwearToGodPleaseSomebodyKillThisNightmare();  // The nightmare never ends
            mapMan.Initialize();
            systems.Initialize();

            deps.Resolve<IReflectionManager>().LoadAssemblies(assemblies);

            var modLoader = deps.Resolve<TestingModLoader>();
            modLoader.Assemblies = contentAssemblies;
            modLoader.TryLoadModulesFrom(ResPath.Root, "");

            entMan.Startup();
            mapMan.Startup();
        }

        [OneTimeTearDown]
        public void BaseTearDown()
        {
            IoCManager.Clear();
        }

        /// <summary>
        /// Called after all IoC registration has been done, but before the graph has been built.
        /// This allows one to add new IoC types or overwrite existing ones if needed.
        /// </summary>
        protected virtual void OverrideIoC()
        {
        }

        protected virtual Assembly[] GetContentAssemblies()
        {
            return Array.Empty<Assembly>();
        }
    }
}
