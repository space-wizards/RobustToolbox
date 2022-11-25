using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Server.Containers;
using Robust.Server.Debugging;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Physics;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using Robust.Shared.Debugging;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

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

            if (Project == UnitTestProject.Client)
            {
                systems.LoadExtraSystemType<ClientMetaDataSystem>();
                systems.LoadExtraSystemType<Robust.Server.Containers.ContainerSystem>();
                systems.LoadExtraSystemType<Robust.Server.GameObjects.TransformSystem>();
                systems.LoadExtraSystemType<Robust.Client.Physics.BroadPhaseSystem>();
                systems.LoadExtraSystemType<Robust.Client.Physics.JointSystem>();
                systems.LoadExtraSystemType<Robust.Client.Physics.PhysicsSystem>();
                systems.LoadExtraSystemType<Robust.Client.Debugging.DebugRayDrawingSystem>();
                systems.LoadExtraSystemType<PrototypeReloadSystem>();
                systems.LoadExtraSystemType<Robust.Client.Debugging.DebugPhysicsSystem>();
            }
            else
            {
                systems.LoadExtraSystemType<ServerMetaDataSystem>();
                systems.LoadExtraSystemType<PVSSystem>();
                systems.LoadExtraSystemType<Robust.Server.Containers.ContainerSystem>();
                systems.LoadExtraSystemType<Robust.Server.GameObjects.TransformSystem>();
                systems.LoadExtraSystemType<BroadPhaseSystem>();
                systems.LoadExtraSystemType<JointSystem>();
                systems.LoadExtraSystemType<PhysicsSystem>();
                systems.LoadExtraSystemType<DebugRayDrawingSystem>();
                systems.LoadExtraSystemType<PrototypeReloadSystem>();
                systems.LoadExtraSystemType<DebugPhysicsSystem>();
            }

            var entMan = deps.Resolve<IEntityManager>();
            var mapMan = deps.Resolve<IMapManager>();

            // Required components for the engine to work
            var compFactory = deps.Resolve<IComponentFactory>();

            if (!compFactory.AllRegisteredTypes.Contains(typeof(MapComponent)))
            {
                compFactory.RegisterClass<MapComponent>();
            }

            if (!compFactory.AllRegisteredTypes.Contains(typeof(MapGridComponent)))
            {
                compFactory.RegisterClass<MapGridComponent>();
            }

            if (!compFactory.AllRegisteredTypes.Contains(typeof(MetaDataComponent)))
            {
                compFactory.RegisterClass<MetaDataComponent>();
            }

            if (!compFactory.AllRegisteredTypes.Contains(typeof(SharedPhysicsMapComponent)))
            {
                compFactory.RegisterClass<PhysicsMapComponent>();
            }

            if (!compFactory.AllRegisteredTypes.Contains(typeof(BroadphaseComponent)))
            {
                compFactory.RegisterClass<BroadphaseComponent>();
            }

            if (!compFactory.AllRegisteredTypes.Contains(typeof(FixturesComponent)))
            {
                compFactory.RegisterClass<FixturesComponent>();
            }

            if (!compFactory.AllRegisteredTypes.Contains(typeof(JointComponent)))
            {
                compFactory.RegisterClass<JointComponent>();
            }

            // So by default EntityManager does its own EntitySystemManager initialize during Startup.
            // We want to bypass this and load our own systems hence we will manually initialize it here.
            entMan.Initialize();
            // RobustUnitTest is complete hot garbage.
            // This makes EventTables ignore *all* the screwed up component abuse it causes.
            entMan.EventBus.OnlyCallOnRobustUnitTestISwearToGodPleaseSomebodyKillThisNightmare();
            mapMan.Initialize();
            systems.Initialize();

            deps.Resolve<IReflectionManager>().LoadAssemblies(assemblies);

            var modLoader = deps.Resolve<TestingModLoader>();
            modLoader.Assemblies = contentAssemblies;
            modLoader.TryLoadModulesFrom(ResourcePath.Root, "");

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
