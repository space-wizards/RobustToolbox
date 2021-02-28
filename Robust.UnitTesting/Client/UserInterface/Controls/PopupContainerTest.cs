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
            var container = new PopupContainer {MinSize = (100, 100)};

            container.Arrange(new UIBox2(0, 0, 100, 100));

            var popup = new Control {MinSize = (50, 50)};

            container.AddChild(popup);

            PopupContainer.SetPopupOrigin(popup, (25, 25));

            container.InvalidateArrange();
            container.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(popup.Position, Is.EqualTo(new Vector2(25, 25)));
            Assert.That(popup.Size, Is.EqualTo(new Vector2(50, 50)));

            // Test that pos gets pushed back to the top left if the size + offset is too large to fit.
            PopupContainer.SetPopupOrigin(popup, (75, 75));

            container.InvalidateArrange();
            container.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(popup.Position, Is.EqualTo(new Vector2(50, 50)));
            Assert.That(popup.Size, Is.EqualTo(new Vector2(50, 50)));

            // Test that pos = 0 if the popup is too large to fit.
            popup.MinSize = (150, 150);

            container.InvalidateArrange();
            container.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(popup.Position, Is.EqualTo(new Vector2(0, 0)));
            Assert.That(popup.Size, Is.EqualTo(new Vector2(150, 150)));
        }

        [Test]
        public void TestAltPos()
        {
            var container = new PopupContainer {MinSize = (100, 100)};

            container.Arrange(new UIBox2(0, 0, 100, 100));

            var popup = new Control {MinSize = (50, 50)};

            container.AddChild(popup);

            PopupContainer.SetPopupOrigin(popup, (75, 75));
            PopupContainer.SetAltOrigin(popup, (65, 25));

            container.InvalidateArrange();
            container.Arrange(new UIBox2(0, 0, 100, 100));

            Assert.That(popup.Position, Is.EqualTo(new Vector2(15, 25)));
            Assert.That(popup.Size, Is.EqualTo(new Vector2(50, 50)));
        }
    }
}
