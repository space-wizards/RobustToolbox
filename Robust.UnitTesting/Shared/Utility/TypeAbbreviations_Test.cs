using System;
using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.Shared.TestTypeAbbreviation
{
    public sealed class Foo<T> {}

    public sealed class Bar {}
}

namespace Robust.UnitTesting.Shared.Utility
{
    [TestFixture]
    [Parallelizable(ParallelScope.Fixtures | ParallelScope.All)]
    [TestOf(typeof(TypeAbbreviation))]
    public sealed class TypeAbbreviations_Test
    {
        private static IEnumerable<(string name, string expected)> NameTestCases { get; } = new[]
        {
            ("Robust.Shared.GameObjects.Foo", "R.Sh.GO.Foo"),
            ("Robust.Client.GameObjects.Foo", "R.C.GO.Foo"),
            ("Content.Client.GameObjects.Foo", "C.C.GO.Foo"),
            ("Robust.Shared.Maths.Vector2", "R.Sh.M.Vector2"),
            ("System.Collections.Generic.List", "S.C.G.List"),
            ("System.Math", "S.Math"),
        };

        [Test]
        public void Test([ValueSource(nameof(NameTestCases))] (string name, string expected) data)
        {
            Assert.That(TypeAbbreviation.Abbreviate(data.name), Is.EqualTo(data.expected));
        }


        private static IEnumerable<(Type type, string expected)> TypeTestCases { get; } = new[]
        {
            ( typeof(Robust.Shared.TestTypeAbbreviation.Foo<Robust.Shared.TestTypeAbbreviation.Bar>)
            , "R.Sh.TestTypeAbbreviation.Foo`1[R.Sh.TestTypeAbbreviation.Bar]"
            ),
            (typeof(Robust.Shared.TestTypeAbbreviation.Bar), "R.Sh.TestTypeAbbreviation.Bar"),
        };

        [Test]
        public void Test([ValueSource(nameof(TypeTestCases))] (Type type, string expected) data)
        {
            Assert.That(TypeAbbreviation.Abbreviate(data.type), Is.EqualTo(data.expected));
        }
    }
}
