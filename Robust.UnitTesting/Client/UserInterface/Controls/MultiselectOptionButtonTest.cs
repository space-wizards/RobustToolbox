using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Robust.UnitTesting.Client.UserInterface.Controls
{
    [TestFixture]
    [TestOf(typeof(MultiselectOptionButton<>))]
    public class MultiselectOptionButtonTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [Test]
        public void TestAddRemoveItem()
        {
            var uiManager = IoCManager.Resolve<IUserInterfaceManagerInternal>();
            uiManager.InitializeTesting();
            var optionButton = new MultiselectOptionButton<string>();
            optionButton.AddItem("Label1", "Key1");
            optionButton.AddItem("Label2", "Key2");
            optionButton.AddItem("Label3", "Key3");

            Assert.That(optionButton.GetIdx("Key1"), Is.EqualTo(0));
            Assert.That(optionButton.GetIdx("Key2"), Is.EqualTo(1));
            Assert.That(optionButton.GetIdx("Key3"), Is.EqualTo(2));
            Assert.That(optionButton.GetItemKey(0), Is.EqualTo("Key1"));
            Assert.That(optionButton.GetItemKey(1), Is.EqualTo("Key2"));
            Assert.That(optionButton.GetItemKey(2), Is.EqualTo("Key3"));

            optionButton.RemoveItem(1);

            Assert.That(optionButton.GetIdx("Key1"), Is.EqualTo(0));
            Assert.That(optionButton.GetIdx("Key3"), Is.EqualTo(1));
            Assert.That(optionButton.GetItemKey(0), Is.EqualTo("Key1"));
            Assert.That(optionButton.GetItemKey(1), Is.EqualTo("Key3"));

            optionButton.RemoveItem(0);

            Assert.That(optionButton.GetIdx("Key3"), Is.EqualTo(0));
            Assert.That(optionButton.GetItemKey(0), Is.EqualTo("Key3"));
        }
    }
}
