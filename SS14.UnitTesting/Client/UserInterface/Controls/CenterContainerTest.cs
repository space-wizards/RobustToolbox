using NUnit.Framework;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.UnitTesting.Client.UserInterface.Controls
{
    [TestFixture]
    [TestOf(typeof(CenterContainer))]
    public class CenterContainerTest : SS14UnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [Test]
        public void Test()
        {
            var container = new CenterContainer {Size = (100, 100)};
            var child = new Control {CustomMinimumSize = (50, 50)};

            container.AddChild(child);
            Assert.That(container.CombinedMinimumSize, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child.Position, Is.EqualTo(new Vector2(25, 25)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
        }
    }
}
