using System;
using System.IO;
using Moq;
using NUnit.Framework;
using Robust.Server.Configuration;
using Robust.Server.Reflection;
using Robust.Server.Serialization;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Profiling;
using Robust.Shared.Reflection;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture, Serializable]
    sealed class EntityState_Tests
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
            container.Register<IConfigurationManager, ServerNetConfigurationManager>();
            container.Register<IConfigurationManagerInternal, ServerNetConfigurationManager>();
            container.Register<INetManager, NetManager>();
            container.Register<IReflectionManager, ServerReflectionManager>();
            container.Register<IRobustSerializer, ServerRobustSerializer>();
            container.Register<IRobustMappedStringSerializer, RobustMappedStringSerializer>();
            container.Register<IAuthManager, AuthManager>();
            container.Register<IGameTiming, GameTiming>();
            container.Register<ProfManager, ProfManager>();
            container.RegisterInstance<IReplayRecordingManager>(new Mock<IReplayRecordingManager>().Object);
            container.BuildGraph();

            var cfg = container.Resolve<IConfigurationManagerInternal>();
            cfg.Initialize(true);
            cfg.LoadCVarsFromAssembly(typeof(IConfigurationManager).Assembly);

            container.Resolve<IReflectionManager>().LoadAssemblies(AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"));

            IoCManager.InitThread(container, replaceExisting: true);

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
                        new ComponentChange(0, new MapGridComponentState(16, chunkData: null), default)
                    }, default);

                serializer.Serialize(stream, payload);
                array = stream.ToArray();
            }

            IoCManager.Clear();

            Assert.Pass($"Size in Bytes: {array.Length.ToString()}");
        }
    }
}
