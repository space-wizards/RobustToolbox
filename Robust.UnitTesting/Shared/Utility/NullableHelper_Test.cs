using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility
{
    [TestFixture]
    [TestOf(typeof(NullableHelper))]
    public class NullableHelper_Test
    {
        [SetUp]
        public void Setup()
        {
            //initializing logmanager so it wont error out if nullablehelper logs an error
            var collection = new DependencyCollection();
            collection.Register<ILogManager, LogManager>();
            collection.BuildGraph();
            IoCManager.InitThread(collection, true);
        }

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
    }

#pragma warning disable 169
#pragma warning disable 414
    public class NullableTestClass
    {
        private int? i;
        private double? d;
        public object? o;
        public INullableTestInterface? Itest;
        public NullableTestClass? nTc;
        private char? c;
    }

    public class NotNullableTestClass
    {
        private int i;
        private double d;
        private object o = null!;
        private INullableTestInterface Itest = null!;
        private NullableTestClass nTc = null!;
        private char c;
    }
#pragma warning restore 414
#pragma warning restore 169

    public interface INullableTestInterface{}
}
