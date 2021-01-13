using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.UserInterface.Controls
{
    [TestFixture]
    [TestOf(typeof(RadioOptions<int>))]
    public class RadioOptionsTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [Test]
        public void TestDefaultInvoke()
        {
            //Arrange
            RadioOptions<int> _optionButton = new RadioOptions<int>(RadioOptionsLayout.Horizontal);

            int itemId = _optionButton.AddItem("High", 1);

            int countSelected = 0;
            _optionButton.OnItemSelected += args =>
            {
                countSelected++;
            };

            //Act
            _optionButton.InvokeItemSelected(new RadioOptionItemSelectedEventArgs<int>(itemId, _optionButton));

            //Assert
            Assert.That(countSelected, Is.EqualTo(1));
        }

        [Test]
        public void TestOverrideInvoke()
        {
            //Arrange
            RadioOptions<int> _optionButton = new RadioOptions<int>(RadioOptionsLayout.Horizontal);

            int countSelected = 0;

            int itemId = _optionButton.AddItem("High", 1, args => { countSelected--; });
            int itemId2 = _optionButton.AddItem("High", 2);

            _optionButton.OnItemSelected += args =>
            {
                countSelected++;
            };

            //Act
            _optionButton.InvokeItemSelected(new RadioOptionItemSelectedEventArgs<int>(itemId, _optionButton));

            //Assert
            Assert.That(countSelected, Is.EqualTo(-1));

            //Act
            _optionButton.InvokeItemSelected(new RadioOptionItemSelectedEventArgs<int>(itemId2, _optionButton));
            _optionButton.InvokeItemSelected(new RadioOptionItemSelectedEventArgs<int>(itemId2, _optionButton));

            //Assert
            Assert.That(countSelected, Is.EqualTo(1));
        }
    }
}
