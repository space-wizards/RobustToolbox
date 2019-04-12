using System;
using NUnit.Framework;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Robust.UnitTesting.Shared.Serialization
{
    [TestFixture]
    class NetSerializableAttribute_Test : SS14UnitTest
    {
        private IReflectionManager _reflection;

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
