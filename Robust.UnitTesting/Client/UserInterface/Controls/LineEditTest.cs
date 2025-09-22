using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Client.UserInterface.Controls
{
    [TestFixture]
    [TestOf(typeof(LineEdit))]
    public sealed class LineEditTest : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Client;

        [Test]
        public void TestRuneBackspace()
        {
            var lineEdit = new TestLineEdit();

            lineEdit.Text = "Foo👏";
            lineEdit.CursorPosition = lineEdit.Text.Length;

            var eventArgs = new GUIBoundKeyEventArgs(
                EngineKeyFunctions.TextBackspace,
                BoundKeyState.Down,
                default, false, default, default);
            lineEdit.KeyBindDown(eventArgs);

            Assert.That(lineEdit.Text, Is.EqualTo("Foo"));
            Assert.That(lineEdit.CursorPosition, Is.EqualTo(3));
        }

        [Test]
        public void TestRuneDelete()
        {
            var lineEdit = new TestLineEdit();

            lineEdit.Text = "Foo👏";
            lineEdit.CursorPosition = 3;

            var eventArgs = new GUIBoundKeyEventArgs(
                EngineKeyFunctions.TextDelete,
                BoundKeyState.Down,
                default, false, default, default);
            lineEdit.KeyBindDown(eventArgs);

            Assert.That(lineEdit.Text, Is.EqualTo("Foo"));
            Assert.That(lineEdit.CursorPosition, Is.EqualTo(3));
        }

        [Test]
        public void TestMoveLeft()
        {
            var lineEdit = new TestLineEdit();

            lineEdit.Text = "Foo👏";
            lineEdit.CursorPosition = lineEdit.Text.Length;

            var eventArgs = new GUIBoundKeyEventArgs(
                EngineKeyFunctions.TextCursorLeft,
                BoundKeyState.Down,
                default, false, default, default);
            lineEdit.KeyBindDown(eventArgs);

            Assert.That(lineEdit.Text, Is.EqualTo("Foo👏"));
            Assert.That(lineEdit.CursorPosition, Is.EqualTo(3));
        }

        [Test]
        public void TestMoveRight()
        {
            var lineEdit = new TestLineEdit();

            lineEdit.Text = "Foo👏";
            lineEdit.CursorPosition = 3;

            var eventArgs = new GUIBoundKeyEventArgs(
                EngineKeyFunctions.TextCursorRight,
                BoundKeyState.Down,
                default, false, default, default);
            lineEdit.KeyBindDown(eventArgs);

            Assert.That(lineEdit.Text, Is.EqualTo("Foo👏"));
            Assert.That(lineEdit.CursorPosition, Is.EqualTo(5));
        }

        [Test]
        public void TestMoveSelectLeft()
        {
            var lineEdit = new TestLineEdit();

            lineEdit.Text = "Foo👏";
            lineEdit.CursorPosition = lineEdit.Text.Length;

            var eventArgs = new GUIBoundKeyEventArgs(
                EngineKeyFunctions.TextCursorSelectLeft,
                BoundKeyState.Down,
                default, false, default, default);
            lineEdit.KeyBindDown(eventArgs);

            Assert.That(lineEdit.Text, Is.EqualTo("Foo👏"));
            Assert.That(lineEdit.SelectionStart, Is.EqualTo(5));
            Assert.That(lineEdit.CursorPosition, Is.EqualTo(3));
        }

        [Test]
        public void TestMoveSelectRight()
        {
            var lineEdit = new TestLineEdit();

            lineEdit.Text = "Foo👏";
            lineEdit.CursorPosition = 3;

            var eventArgs = new GUIBoundKeyEventArgs(
                EngineKeyFunctions.TextCursorSelectRight,
                BoundKeyState.Down,
                default, false, default, default);
            lineEdit.KeyBindDown(eventArgs);

            Assert.That(lineEdit.Text, Is.EqualTo("Foo👏"));
            Assert.That(lineEdit.SelectionStart, Is.EqualTo(3));
            Assert.That(lineEdit.CursorPosition, Is.EqualTo(5));
        }

        [Test]
        // RIGHT
        [TestCase("Foo Bar Baz", false, 0, ExpectedResult = 3)]
        [TestCase("Foo Bar Baz", false, 8, ExpectedResult = 11)]
        [TestCase("Foo[Bar[Baz", false, 0, ExpectedResult = 3)]
        [TestCase("Foo[Bar[Baz", false, 3, ExpectedResult = 4)]
        [TestCase("Foo^Bar^Baz", false, 0, ExpectedResult = 3)]
        [TestCase("Foo^Bar^Baz", false, 3, ExpectedResult = 5)]
        [TestCase("Foo^^^Bar^Baz", false, 3, ExpectedResult = 9)]
        [TestCase("^^^ ^^^", false, 0, ExpectedResult = 6)]
        [TestCase("^^^ ^^^", false, 7, ExpectedResult = 13)]
        // LEFT
        [TestCase("Foo Bar Baz", true, 4, ExpectedResult = 0)]
        [TestCase("Foo Bar Baz", true, 11, ExpectedResult = 8)]
        [TestCase("Foo[Bar[Baz", true, 3, ExpectedResult = 0)]
        [TestCase("Foo[Bar[Baz", true, 4, ExpectedResult = 3)]
        [TestCase("Foo^Bar^Baz", true, 3, ExpectedResult = 0)]
        [TestCase("Foo^Bar^Baz", true, 5, ExpectedResult = 3)]
        [TestCase("Foo^^^Bar^Baz", true, 9, ExpectedResult = 3)]
        [TestCase("^^^ ^^^", true, 7, ExpectedResult = 0)]
        [TestCase("^^^ ^^^", true, 13, ExpectedResult = 7)]
        public int TestMoveWord(string text, bool left, int initCursorPos)
        {
            // ^ is replaced by 👏 because Rider refuses to run the tests otherwise.
            text = text.Replace("^", "👏");

            var lineEdit = new TestLineEdit();

            lineEdit.Text = text;
            lineEdit.CursorPosition = initCursorPos;

            var eventArgs = new GUIBoundKeyEventArgs(
                left ? EngineKeyFunctions.TextCursorWordLeft : EngineKeyFunctions.TextCursorWordRight,
                BoundKeyState.Down,
                default, false, default, default);
            lineEdit.KeyBindDown(eventArgs);

            return lineEdit.CursorPosition;
        }


        private sealed class TestLineEdit : LineEdit
        {
            public override bool HasKeyboardFocus()
            {
                return true;
            }
        }
    }
}
