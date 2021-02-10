using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.UserInterface.Controls
{
    [TestFixture]
    [TestOf(typeof(CenterContainer))]
    public class CenterContainerTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [Test]
        public void Test()
        {
            var container = new CenterContainer {Size = (100, 100)};
            var child = new Control {CustomMinimumSize = (50, 50)};

            container.AddChild(child);

            container.ForceRunLayoutUpdate();

            Assert.That(container.CombinedMinimumSize, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(child.Position, Is.EqualTo(new Vector2(25, 25)));
            Assert.That(child.Size, Is.EqualTo(new Vector2(50, 50)));
        }
    }
}
