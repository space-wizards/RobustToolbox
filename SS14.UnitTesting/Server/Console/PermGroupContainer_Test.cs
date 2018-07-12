using System;
using System.IO;
using System.Text;
using Moq;
using NUnit.Framework;
using SS14.Server.Console;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.Utility;

namespace SS14.UnitTesting.Server.Console
{
    [TestFixture]
    [TestOf(typeof(ConGroupContainer))]
    class PermGroupContainer_Test
    {
        [Test]
        public void LoadYamlContent()
        {
            //Arrange
            var contentStream = new MemoryStream();
            var writer = new StreamWriter(contentStream);
            writer.Write(yamlDoc);
            writer.Flush();
            contentStream.Position = 0;

            var filePath = new ResourcePath(@"/Groups/groups.yml");

            var mockRes = new Mock<IResourceManager>();
            var mockData = new Mock<IWritableDirProvider>();

            mockData.Setup(data => data.Exists(It.Is<ResourcePath>(path => path.Equals(filePath)))).Returns(false);
            mockRes.SetupGet(res => res.UserData).Returns(mockData.Object);
            mockRes.Setup(res => res.TryContentFileRead(It.IsAny<ResourcePath>(), out contentStream)).Returns(true);

            var container = new ConGroupContainer(mockRes.Object, SawmillFactory());

            //Act
            container.LoadGroups();

            //Assert
            Assert.That(contentStream.CanRead, Is.False);
            Assert.That(container.Groups.ContainsKey(new ConGroupIndex(1)), Is.True);
            Assert.That(container.Groups[new ConGroupIndex(1)].Commands.Contains("list"), Is.True);
        }

        [Test]
        public void LoadYamlData()
        {
            //Arrange
            var contentStream = new MemoryStream();
            var writer = new StreamWriter(contentStream);
            writer.Write(yamlDoc);
            writer.Flush();
            contentStream.Position = 0;

            var filePath = new ResourcePath(@"/Groups/groups.yml");

            var mockRes = new Mock<IResourceManager>();
            var mockData = new Mock<IWritableDirProvider>();

            mockData.Setup(data => data.Exists(It.Is<ResourcePath>(path => path.Equals(filePath)))).Returns(true);
            mockData.Setup(data => data.Open(It.IsAny<ResourcePath>(), It.IsAny<FileMode>())).Returns(contentStream);
            mockRes.SetupGet(res => res.UserData).Returns(mockData.Object);

            var container = new ConGroupContainer(mockRes.Object, SawmillFactory());

            //Act
            container.LoadGroups();

            //Assert
            contentStream.Close();

            Assert.That(contentStream.CanRead, Is.False);
            Assert.That(container.Groups.ContainsKey(new ConGroupIndex(1)), Is.True);
            Assert.That(container.Groups[new ConGroupIndex(1)].Commands.Contains("list"), Is.True);
        }

        [Test]
        public void SaveYaml()
        {
            //Arrange
            var contentStream = new MemoryStream();
            var writer = new StreamWriter(contentStream, Encoding.Unicode);
            writer.Write(yamlDoc);
            writer.Flush();
            var backingArray = new byte[contentStream.Position * 4]; //4x the space for wiggle room
            contentStream.Position = 0;

            var filePath = new ResourcePath(@"/Groups/groups.yml");

            var writeStream = new MemoryStream(backingArray);

            var mockRes = new Mock<IResourceManager>();
            var mockData = new Mock<IWritableDirProvider>();

            mockData.Setup(data => data.Exists(It.Is<ResourcePath>(path => path.Equals(filePath)))).Returns(false);
            mockData.Setup(data => data.Open(It.IsAny<ResourcePath>(), It.IsAny<FileMode>())).Returns(writeStream); // the 'New' File
            mockRes.SetupGet(res => res.UserData).Returns(mockData.Object);
            mockRes.Setup(res => res.TryContentFileRead(It.Is<ResourcePath>(path => path.Equals(filePath)), out contentStream)).Returns(true);

            var container = new ConGroupContainer(mockRes.Object, SawmillFactory());
            container.LoadGroups();

            //Act
            container.SaveGroups();

            //Assert
            Assert.That(contentStream.CanRead, Is.False);
            Assert.That(writeStream.CanRead, Is.False);

            var yamlResult = new StreamReader(new MemoryStream(backingArray)).ReadToEnd().TrimEnd('\0');
            Assert.That(yamlResult, Is.EqualTo(yamlDoc));
        }

        private ISawmill SawmillFactory()
        {
            var mockMill = new Mock<ISawmill>();

            return mockMill.Object;
        }
        
        private const string yamlDoc = @"- Index: 1
  Name: Player
  Commands:
  - help
  - list
";
    }
}
