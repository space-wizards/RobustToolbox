using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Robust.Server.Reflection;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
            container.Register<INetManager, NetManager>();
            container.Register<IReflectionManager, ServerReflectionManager>();
            container.Register<IRobustSerializer, RobustSerializer>();
            container.BuildGraph();

            container.Resolve<IReflectionManager>().LoadAssemblies(AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"));

            IoCManager.InitThread(container);

            container.Resolve<INetManager>().Initialize(true);

            var serializer = container.Resolve<IRobustSerializer>();
            serializer.Initialize();

            byte[] array;
            using(var stream = new MemoryStream())
            {
                var payload = new EntityState(new EntityUid(512), Array.Empty<ComponentChanged>(), Array.Empty<ComponentState>());

                serializer.Serialize(stream, payload);
                array = stream.ToArray();
            }

            IoCManager.Clear();

            Assert.Pass($"Size in Bytes: {array.Length.ToString()}");
        }
    }
}
