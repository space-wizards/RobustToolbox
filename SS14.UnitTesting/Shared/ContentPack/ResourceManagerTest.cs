using System.IO;
using NUnit.Framework;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.IoC;
using SS14.Shared.Utility;

namespace SS14.UnitTesting.Shared.ContentPack
{
    [TestFixture]
    public class ResourceManagerTest : SS14UnitTest
    {
        private static readonly byte[] Data =
        {
            0x56, 0x75, 0x6c, 0x6b, 0x61, 0x6e, 0x20, 0x57, 0x68, 0x65, 0x6e
        };

        [OneTimeSetUp]
        public void Setup()
        {
            var stream = new MemoryStream(Data);
            var resourceManager = IoCManager.Resolve<IResourceManagerInternal>();
            resourceManager.MountStreamAt(stream, new ResourcePath("/a/b/c.dat"));
        }

        [Test]
        public void TestMountedStreamRead()
        {
            var resourceManager = IoCManager.Resolve<IResourceManagerInternal>();
            using (var stream = resourceManager.ContentFileRead("/a/b/c.dat"))
            {
                Assert.That(stream.ToArray(), Is.EqualTo(Data));
            }
        }
    }
}
