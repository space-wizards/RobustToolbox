using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    public abstract class TypeSerializerTest : RobustUnitTest
    {
        public ISerializationManager Serialization => IoCManager.Resolve<ISerializationManager>();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Serialization.Initialize();
        }
    }
}
