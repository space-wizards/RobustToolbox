using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;

namespace Robust.UnitTesting.Shared.Serialization
{
    public abstract class SerializationTest : RobustUnitTest
    {
        protected ISerializationManager Serialization => IoCManager.Resolve<ISerializationManager>();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Serialization.Initialize();
        }
    }
}
