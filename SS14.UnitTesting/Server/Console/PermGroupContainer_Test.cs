using System;
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
            Assert.That(container.Groups.ContainsKey(1), Is.True);
            Assert.That(container.Groups[1].Commands.Contains("list"), Is.True);
        }

        [Test]
        public void SaveYamlTest()
        {
            //TODO: IResourceManager needs to handle reading/writing to the user data folder.
            //Until then, there is no way to mock this.
            
            //Arrange
            var resMan = ResManFactory();
            var container = new PermGroupContainer();
            container.LoadGroups(resMan);

            //Act
            container.SaveGroups(resMan);

            //Assert
            Assert.That(true, Is.True);
            
        }

        private IResourceManager ResManFactory()
        {
            var mock = new Mock<IResourceManager>();
            
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(yamlDoc);
            writer.Flush();
            stream.Position = 0;

            mock.Setup(foo => foo.ConfigDirectory).Returns(AppDomain.CurrentDomain.BaseDirectory);
            mock.Setup(res => res.TryContentFileRead(It.IsAny<ResourcePath>(), out stream)).Returns(true);

            return mock.Object;
        }

        private const string yamlDoc = @"---
- Index: 1
  Name: Player
  Commands:
  - help
  - list

";
    }
}
