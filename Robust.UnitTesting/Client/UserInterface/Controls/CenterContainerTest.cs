using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.UserInterface.Controls
{
    [TestFixture]
    [TestOf(typeof(CenterContainer))]
    public sealed class CenterContainerTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [Test]
        public void Test()
        {
            var container = new CenterContainer();
            var child = new Control {MinSize = (50, 50)};

            container.AddChild(child);

            container.Arrange(UIBox2.FromDimensions(0, 0, 100, 100));

            Assert.That(container.DesiredSize, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child.Position, Is.EqualTo(new Vector2(25, 25)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
        }
    }
}
