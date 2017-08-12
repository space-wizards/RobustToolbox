using NUnit.Framework;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.IoC.Exceptions;
using System;

namespace SS14.UnitTesting.SS14.Shared.IoC
{
    [TestFixture]
    public class IoCManager_Test : SS14UnitTest
    {
        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Register<IIoCTestPriories, IoCTestPriorities2>();
            IoCManager.Register<IIoCTestPriories, IoCTestPriorities1>(true);
            IoCManager.Register<TestFieldInjection, TestFieldInjection>();

            IoCManager.BuildGraph();
        }

        [Test]
        public void IoCTestFieldInjection()
        {
            var tester = IoCManager.Resolve<TestFieldInjection>();
            tester.Test();
        }

        [Test]
        public void IoCTestBasic()
        {
            Assert.That(IoCManager.Resolve<IEntityManager>(),
                        Is.Not.Null,
                        "IoC failed to return an IServerConfigurationManager.");

            Assert.That(IoCManager.Resolve<IServerEntityManager>(),
                        Is.Not.Null,
                        "IoC failed to return an IResourceCache.");
        }

        [Test]
        public void IoCTestOverwrite()
        {
            Assert.That(IoCManager.Resolve<IIoCTestPriories>(), Is.TypeOf<IoCTestPriorities1>());
        }

        [Test]
        public void IoCTestConstructorException()
        {
            IoCManager.Register<IConstructorException, ConstructorException>();
            Assert.That(() => IoCManager.BuildGraph(), Throws.InstanceOf<ImplementationConstructorException>().And.InnerException.InstanceOf<TestConstructorExceptionException>());
        }
    }

    public interface IIoCFailInterface { }

    public interface IIoCTestPriories { }

    public class IoCTestPriorities1 : IIoCTestPriories { }
    public class IoCTestPriorities2 : IIoCTestPriories { }

    public interface IConstructorException { }

    public class ConstructorException : IConstructorException
    {
        public ConstructorException()
        {
            throw new TestConstructorExceptionException();
        }
    }

    public class TestConstructorExceptionException : Exception
    {
    }

    public class TestFieldInjection
    {
        [Dependency]
        private readonly TestFieldInjection myself;

        public void Test()
        {
            Assert.That(myself, Is.EqualTo(this));
        }
    }
}
