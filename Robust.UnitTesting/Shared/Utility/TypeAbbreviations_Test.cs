using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility
{
    [TestFixture]
    [Parallelizable(ParallelScope.Fixtures | ParallelScope.All)]
    [TestOf(typeof(TypeAbbreviation))]
    public class TypeAbbreviations_Test
    {
        private static IEnumerable<(string name, string expected)> TestCases { get; } = new[]
        {
            ("Robust.Shared.GameObjects.Foo", "R.Sh.GO.Foo"),
            ("Robust.Client.GameObjects.Foo", "R.C.GO.Foo"),
            ("Content.Client.GameObjects.Foo", "C.C.GO.Foo"),
            ("Robust.Shared.Maths.Vector2", "R.Sh.M.Vector2"),
            ("System.Collections.Generic.List", "S.C.G.List"),
            ("System.Math", "S.Math"),
        };

        [Test]
        public void Test([ValueSource(nameof(TestCases))] (string name, string expected) data)
        {
            Assert.That(TypeAbbreviation.Abbreviate(data.name), Is.EqualTo(data.expected));
        }
    }
}
