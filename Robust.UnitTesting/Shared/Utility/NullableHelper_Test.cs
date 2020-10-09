using System;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility
{
    [TestFixture]
    [TestOf(typeof(NullableHelper))]

    public class NullableHelper_Test
    {
        [Test]
        public void IsNullableTest()
        {
            var fields = typeof(NullableTestClass).GetAllFields();
            foreach (var field in fields)
            {
                Assert.That(NullableHelper.IsMarkedAsNullable(field), Is.True, $"{field}");
            }
        }

        [Test]
        public void IsNotNullableTest()
        {
            var fields = typeof(NotNullableTestClass).GetAllFields();
            foreach (var field in fields)
            {
                Assert.That(!NullableHelper.IsMarkedAsNullable(field), Is.True, $"{field}");
            }
        }

        private class NullableTestClass
        {
            private int? i;
            private double? d;
            //todo find out why this doesn't work -> private object? o;
            private NullableTestClass? nTc;
            private char? c;
        }

        private class NotNullableTestClass
        {
            private int i;
            private double d;
            private object o = null!;
            private NullableTestClass nTc = null!;
            private char c;
        }
    }
}
