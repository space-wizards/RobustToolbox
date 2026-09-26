using NUnit.Framework;
using Robust.Client.Input;
using Key = Robust.Client.Input.Keyboard.Key;
using PackedKeyCombo = Robust.Client.Input.InputManager.PackedKeyCombo;

namespace Robust.Client.Tests.Input
{
    // Windows fakes AltGr as left control + right alt so a binding on control or alt would
    // fire for what the user typed as a plain character. See https://github.com/space-wizards/RobustToolbox/issues/6592
    [TestFixture, Parallelizable]
    public sealed class AltGrKeyBindingTest
    {
        private static bool[] Pressed(params Key[] keys)
        {
            var pressed = new bool[256];
            foreach (var key in keys)
            {
                pressed[(int)key] = true;
            }

            return pressed;
        }

        [Test]
        public void ControlComboMatchesWithoutAltGr()
        {
            var combo = new PackedKeyCombo(Key.A, Key.Control);

            Assert.That(InputManager.PackedMatchesPressedState(combo, Pressed(Key.A, Key.Control), false), Is.True);
        }

        [Test]
        public void ControlComboDoesNotMatchUnderAltGr()
        {
            var combo = new PackedKeyCombo(Key.A, Key.Control);

            Assert.That(InputManager.PackedMatchesPressedState(combo, Pressed(Key.A, Key.Control, Key.Alt), true), Is.False);
        }

        [Test]
        public void AltComboDoesNotMatchUnderAltGr()
        {
            var combo = new PackedKeyCombo(Key.A, Key.Alt);

            Assert.That(InputManager.PackedMatchesPressedState(combo, Pressed(Key.A, Key.Control, Key.Alt), true), Is.False);
        }

        [Test]
        public void UnmodifiedComboStillMatchesUnderAltGr()
        {
            var combo = new PackedKeyCombo(Key.A);

            Assert.That(InputManager.PackedMatchesPressedState(combo, Pressed(Key.A, Key.Control, Key.Alt), true), Is.True);
        }

        [Test]
        public void ShiftComboStillMatchesUnderAltGr()
        {
            var combo = new PackedKeyCombo(Key.A, Key.Shift);

            Assert.That(InputManager.PackedMatchesPressedState(combo, Pressed(Key.A, Key.Shift, Key.Control, Key.Alt), true), Is.True);
        }
    }
}
