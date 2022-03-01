using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Physics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;
using GridFixtureSystem = Robust.Client.GameObjects.GridFixtureSystem;

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
            IoCManager.InitThread();
            IoCManager.Clear();

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

            var configurationManager = IoCManager.Resolve<IConfigurationManagerInternal>();

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

            var systems = IoCManager.Resolve<IEntitySystemManager>();
            // Required systems
            systems.LoadExtraSystemType<ContainerSystem>();
            systems.LoadExtraSystemType<TransformSystem>();

            var entMan = IoCManager.Resolve<IEntityManager>();
            var mapMan = IoCManager.Resolve<IMapManager>();

            // So by default EntityManager does its own EntitySystemManager initialize during Startup.
            // We want to bypass this and load our own systems hence we will manually initialize it here.
            entMan.Initialize();
            mapMan.Initialize();
            systems.Initialize();

            // TODO: Make this a system and it should be covered off by the above.
            IoCManager.Resolve<IEntityLookup>().Startup();

            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(assemblies);

            var modLoader = IoCManager.Resolve<TestingModLoader>();
            modLoader.Assemblies = contentAssemblies;
            modLoader.TryLoadModulesFrom(ResourcePath.Root, "");

            // Required components for the engine to work
            var compFactory = IoCManager.Resolve<IComponentFactory>();

            if (!compFactory.AllRegisteredTypes.Contains(typeof(MetaDataComponent)))
            {
                compFactory.RegisterClass<MetaDataComponent>();
            }

            if (!compFactory.AllRegisteredTypes.Contains(typeof(EntityLookupComponent)))
            {
                compFactory.RegisterClass<EntityLookupComponent>();
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
