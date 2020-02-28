using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.UserInterface.Controls
{
    [TestFixture]
    [TestOf(typeof(PopupContainer))]
    public class PopupContainerTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [Test]
        public void Test()
        {
            var wrap = new LayoutContainer();
            var container = new PopupContainer {CustomMinimumSize = (100, 100)};
            wrap.AddChild(container);

            // Need this wrapper so that container has the correct size.
            wrap.ForceRunLayoutUpdate();

            var popup = new Control {CustomMinimumSize = (50, 50)};

            container.AddChild(popup);

            PopupContainer.SetPopupOrigin(popup, (25, 25));

            container.ForceRunLayoutUpdate();

            Assert.That(popup.Position, Is.EqualTo(new Vector2(25, 25)));
            Assert.That(popup.Size, Is.EqualTo(new Vector2(50, 50)));

            // Test that pos gets pushed back to the top left if the size + offset is too large to fit.
            PopupContainer.SetPopupOrigin(popup, (75, 75));

            container.ForceRunLayoutUpdate();

            Assert.That(popup.Position, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(popup.Size, Is.EqualTo(new Vector2(50, 50)));

            // Test that pos = 0 if the popup is too large to fit.
            popup.CustomMinimumSize = (150, 150);

            container.ForceRunLayoutUpdate();

            Assert.That(popup.Position, Is.EqualTo(new Vector2(0, 0)));
            Assert.That(popup.Size, Is.EqualTo(new Vector2(150, 150)));
        }
    }
}
