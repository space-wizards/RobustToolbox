using NUnit.Framework;
using SS14.Client.Interfaces.Resource;
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
            IoCManager.Register<IIoCNoPublicConstructorTest, IoCNoPublicConstructorTest>();
            IoCManager.Register<IIoCTestPriories, IoCTestPriorities2>();
            IoCManager.Register<IIoCTestPriories, IoCTestPriorities1>(true);
            IoCManager.Register<IIoCCircularDeps1, CircularDeps1>();
            IoCManager.Register<IIoCCircularDeps2, CircularDeps2>();
            IoCManager.Register<IConstructorException, ConstructorException>();
        }

        [Test]
        public void IoCTestBasic()
        {
            Assert.That(IoCManager.Resolve<IConfigurationManager>(),
                        Is.Not.Null,
                        "IoC failed to return an IServerConfigurationManager.");

            Assert.That(IoCManager.Resolve<IResourceManager>(),
                        Is.Not.Null,
                        "IoC failed to return an IResourceManager.");
        }

        [Test]
        public void IoCTestExceptions()
        {
            Assert.That(() => IoCManager.Resolve<IIoCFailInterface>(),
                        Throws.TypeOf<MissingImplementationException>(),
                        "IoC did not throw a MissingImplementationException.");

            Assert.That(() => IoCManager.Resolve<IIoCNoPublicConstructorTest>(),
                        Throws.TypeOf<NoPublicConstructorException>(),
                        "IoC did not throw a NoPublicConstructorException.");
        }

        [Test]
        public void IoCTestOverwrite()
        {
            Assert.That(IoCManager.Resolve<IIoCTestPriories>(), Is.TypeOf<IoCTestPriorities1>());
        }

        [Test]
        public void IoCTestCircularDependencies()
        {
            Assert.That(() => IoCManager.Resolve<IIoCCircularDeps1>(), Throws.InstanceOf<CircularDependencyException>());
        }

        [Test]
        public void IoCTestConstructorException()
        {
            Assert.That(() => IoCManager.Resolve<IConstructorException>(), Throws.InstanceOf<ImplementationConstructorException>().And.InnerException.InstanceOf<TestConstructorExceptionException>());
        }
    }

    public interface IIoCFailInterface { }

    public interface IIoCNoPublicConstructorTest { }

    public class IoCNoPublicConstructorTest : IIoCNoPublicConstructorTest
    {
        private IoCNoPublicConstructorTest()
        {
        }
    }

    public interface IIoCTestPriories { }

    public class IoCTestPriorities1 : IIoCTestPriories { }
    public class IoCTestPriorities2 : IIoCTestPriories { }

    public interface IIoCCircularDeps1 { }
    public interface IIoCCircularDeps2 { }

    public class CircularDeps1 : IIoCCircularDeps1
    {
        public CircularDeps1(IIoCCircularDeps2 deps2)
        {
        }
    }

    public class CircularDeps2 : IIoCCircularDeps2
    {
        public CircularDeps2(IIoCCircularDeps1 deps1)
        {
        }
    }

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
}
