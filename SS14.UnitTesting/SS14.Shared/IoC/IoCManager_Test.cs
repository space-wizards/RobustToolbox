using NUnit.Framework;
using SS14.Client.Interfaces.Resource;
using SS14.Server.Interfaces.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.IoC.Exceptions;

namespace SS14.UnitTesting.SS14.Shared.IoC
{
    [TestFixture]
    public class IoCManager_Test : SS14UnitTest
    {
        [Test]
        public void IoCTestBasic()
        {
            Assert.That(IoCManager.Resolve<IServerConfigurationManager>(),
                        Is.Not.Null,
                        "IoC failed to return an IServerConfigurationManager.");

            Assert.That(IoCManager.Resolve<IResourceManager>(),
                        Is.Not.Null,
                        "IoC failed to return an IResourceManager.");
        }

        [Test]
        public void IoCTestExceptions()
        {
            Assert.That(() => IoCManager.ResolveType<IIoCFailInterface>(),
                        Throws.TypeOf<MissingImplementationException>(),
                        "IoC did not throw a MissingImplementationException.");

            Assert.That(() => IoCManager.Resolve<IIoCNoPublicConstructorTest>(),
                        Throws.TypeOf<NoPublicConstructorException>(),
                        "IoC did not throw a NoPublicConstructorException.");
        }

        [Test]
        public void IoCTestAttributes()
        {
            Assert.That(IoCManager.Resolve<IIoCTestPriories>(), Is.TypeOf<IoCTestPriorities1>());
        }
    }

    public interface IIoCFailInterface : IIoCInterface {}

    public interface IIoCNoPublicConstructorTest : IIoCInterface {}

    [IoCTarget]
    public class IoCNoPublicConstructorTest : IIoCNoPublicConstructorTest
    {
        private IoCNoPublicConstructorTest() {}
    }

    public interface IIoCTestPriories : IIoCInterface {}

    [IoCTarget(Priority=10)]
    public class IoCTestPriorities1 : IIoCTestPriories {}
    [IoCTarget]
    public class IoCTestPriorities2 : IIoCTestPriories {}
    [IoCTarget(Priority=30, Disabled=true)]
    public class IoCTestPriorities3 : IIoCTestPriories {}
}
