using System;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;

namespace Robust.UnitTesting.Shared.Serialization
{
    [TestFixture]
    sealed class NetSerializableAttribute_Test : RobustUnitTest
    {
        private IReflectionManager _reflection = default!;

        [OneTimeSetUp]
        public void TestFixtureSetup()
        {
            _reflection = IoCManager.Resolve<IReflectionManager>();
        }

        [Test]
        public void AllNetSerializableObjectsHaveSerializableAttribute()
        {
            var types = _reflection.FindTypesWithAttribute<NetSerializableAttribute>();

            foreach (var type in types)
            {
                Assert.IsTrue(Attribute.IsDefined(type, typeof(NetSerializableAttribute), true),
                    $"{type.FullName} has {nameof(NetSerializableAttribute)}, but not the required {nameof(SerializableAttribute)}.");
            }

        }
    }
}
