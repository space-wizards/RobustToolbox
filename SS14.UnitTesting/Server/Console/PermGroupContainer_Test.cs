using System.IO;
using Moq;
using NUnit.Framework;
using SS14.Server.Console;
using SS14.Shared.Interfaces;
using SS14.Shared.Utility;

namespace SS14.UnitTesting.Server.Console
{
    [TestFixture]
    [TestOf(typeof(PermGroupContainer))]
    class PermGroupContainer_Test
    {
        [Test]
        public void LoadYamlTest()
        {
            //Arrange
            var resMan = ResManFactory();
            var container = new PermGroupContainer();

            //Act
            container.LoadGroups(resMan);

            //Assert
            Assert.That(container.Groups.ContainsKey(0), Is.True);
            Assert.That(container.Groups[0].Commands.Contains("list"), Is.True);
        }

        private IResourceManager ResManFactory()
        {
            var mock = new Mock<IResourceManager>();
            
            var yamlDoc = @"---
- Index: 0
  Name: Player
  Commands:
  - help
  - list

";

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(yamlDoc);
            writer.Flush();
            stream.Position = 0;
            
            mock.Setup(res => res.TryContentFileRead(It.IsAny<ResourcePath>(), out stream)).Returns(true);

            return mock.Object;
        }
    }
}
