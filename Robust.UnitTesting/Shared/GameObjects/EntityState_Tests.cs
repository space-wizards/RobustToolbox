using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Robust.Server.Reflection;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture, Serializable]
    class EntityState_Tests
    {
        /// <summary>
        ///     Used to measure the size of <see cref="object"/>s in bytes. This is not actually a test,
        ///     but a useful benchmark tool, so i'm leaving it here.
        /// </summary>
        [Test]
        public void ComponentChangedSerialized()
        {
            var container = new DependencyCollection();
            container.Register<ILogManager, LogManager>();
            container.Register<IConfigurationManager, ConfigurationManager>();
            container.Register<IConfigurationManagerInternal, ConfigurationManager>();
            container.Register<INetManager, NetManager>();
            container.Register<IReflectionManager, ServerReflectionManager>();
            container.Register<IRobustSerializer, RobustSerializer>();
            container.Register<IRobustMappedStringSerializer, RobustMappedStringSerializer>();
            container.Register<IAuthManager, AuthManager>();
            container.BuildGraph();

            var cfg = container.Resolve<IConfigurationManagerInternal>();
            cfg.Initialize(true);
            cfg.LoadCVarsFromAssembly(typeof(IConfigurationManager).Assembly);

            container.Resolve<IReflectionManager>().LoadAssemblies(AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"));

            IoCManager.InitThread(container);

            cfg.LoadCVarsFromAssembly(typeof(IConfigurationManager).Assembly); // Robust.Shared

            container.Resolve<INetManager>().Initialize(true);

            var serializer = container.Resolve<IRobustSerializer>();
            serializer.Initialize();
            IoCManager.Resolve<IRobustMappedStringSerializer>().LockStrings();

            byte[] array;
            using(var stream = new MemoryStream())
            {
                var payload = new EntityState(
                    new EntityUid(512),
                    new []
                    {
                        new ComponentChanged(false, NetIDs.MAP_GRID, nameof(MapGridComponent)),
                    },
                    new []
                    {
                        new MapGridComponentState(new GridId(0), true),
                    });

                serializer.Serialize(stream, payload);
                array = stream.ToArray();
            }

            IoCManager.Clear();

            Assert.Pass($"Size in Bytes: {array.Length.ToString()}");
        }
    }
}
