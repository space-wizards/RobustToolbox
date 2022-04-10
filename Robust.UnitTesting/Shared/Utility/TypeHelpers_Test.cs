using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility
{
    [TestFixture]
    [TestOf(typeof(TypeHelpers))]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public sealed class TypeHelpers_Test
    {
        [Test]
        public void TestIsBasePropertyDefinition()
        {
            // Easy, the definition of the virtual property.
            Assert.That(typeof(Parent).GetProperty("X")!.IsBasePropertyDefinition(), Is.True);
            Assert.That(typeof(Child).GetProperty("X")!.IsBasePropertyDefinition(), Is.False);
            Assert.That(typeof(SealedChild).GetProperty("X")!.IsBasePropertyDefinition(), Is.False);
            Assert.That(typeof(Hidden).GetProperty("X")!.IsBasePropertyDefinition(), Is.True);
        }

        [Virtual]
        private class Parent
        {
            public virtual int X => 0;
        }

        [Virtual]
        private class Child : Parent
        {
            public override int X => 5;
        }

        [Virtual]
        private class SealedChild : Parent
        {
            public sealed override int X => 6;
        }

        [Virtual]
        private class Hidden : Parent
        {
            public new int X { get; } = 5;
        }
    }
}
