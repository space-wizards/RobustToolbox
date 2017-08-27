using System;
using NUnit.Framework;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Serialization;

namespace SS14.UnitTesting.Shared.Serialization
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
