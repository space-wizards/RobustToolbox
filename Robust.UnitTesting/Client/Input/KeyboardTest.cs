using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Robust.Client.Input;

namespace Robust.UnitTesting.Client.Input
{
    [Parallelizable(ParallelScope.All)]
    public class KeyboardTest
    {
        public static IEnumerable<object[]> TestCases =>
            ((Keyboard.Key[]) Enum.GetValues(typeof(Keyboard.Key)))
            .Where(p => p.ToString().Contains("Mouse"))
            .Select(p => new object[] {p, true});

        [Test]
        [TestCaseSource(nameof(TestCases))]
        [TestCase(Keyboard.Key.A, false)]
        public void TestKeyboardIsMouseKey(Keyboard.Key key, bool isMouse)
        {
            Assert.That(key.IsMouseKey(), Is.EqualTo(isMouse));
        }
    }
}
