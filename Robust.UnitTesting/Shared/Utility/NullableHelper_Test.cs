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
            private object? o;
            private ITestInterace? Itest;
            private NullableTestClass? nTc;
            private char? c;
        }

        private class NotNullableTestClass
        {
            private int i;
            private double d;
            private object o = null!;
            private ITestInterace Itest = null!;
            private NullableTestClass nTc = null!;
            private char c;
        }

        private interface ITestInterace{}
    }
}
