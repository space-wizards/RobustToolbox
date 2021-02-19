using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    public abstract class TypeSerializerTest : RobustUnitTest
    {
        public IServ3Manager Serialization => IoCManager.Resolve<IServ3Manager>();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Serialization.Initialize();
        }
    }
}
