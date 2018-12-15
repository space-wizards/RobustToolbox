using NUnit.Framework;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.IoC.Exceptions;
using System;

namespace SS14.UnitTesting.Shared.IoC
{
    [TestFixture]
    [TestOf(typeof(IoCManager))]
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

        [Test]
        public void IoCTestUnregisteredDirectInject()
        {
            Assert.That(()=>IoCManager.InjectDependencies(new TestUnregisteredInjection()), Throws.TypeOf<UnregisteredDependencyException>());
        }

        [Test]
        public void IoCTestResolveTypeThrowsWhenGraphUnbuilt()
        {
            IoCManager.Register<IIoCFailInterface, TestFailImplementation>();
            // we *forgot* to call BuildGraph
            Assert.That(()=>IoCManager.Resolve<IIoCFailInterface>(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void IoCTestResolveThrowsUnregisteredType()
        {
            Assert.That(()=>IoCManager.Resolve<IIoCFailInterface>(), Throws.TypeOf<UnregisteredTypeException>());
        }

        [Test]
        public void IoCTestDuplicateResgisterWithoutBuildingGraph()
        {
            IoCManager.Register<IIoCFailInterface, TestFailImplementation>();
            IoCManager.BuildGraph();

            // cannot overwrite an already built implementation.
            Assert.That(()=> IoCManager.Register<IIoCFailInterface, TestFailImplementation>(true), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void IoCTestDumplicateRegisterThrows()
        {
            IoCManager.Register<IIoCFailInterface, TestFailImplementation>();

            // duplicate registerations will throw an exception.
            Assert.That(() => IoCManager.Register<IIoCFailInterface, TestFailImplementation>(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void IoCTestCatchExceptionFromDisposingService()
        {
            IoCManager.Register<ITestDisposeExceptionCaught, TestDisposeExceptionCaught>();
            IoCManager.BuildGraph();

            IoCManager.Clear(); // should NOT throw an exception
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

    public class TestFieldInjectionParent
    {
        [Dependency]
        private readonly TestFieldInjection myself;

        [Dependency]
        public TestFieldInjection myotherself;

        public virtual void Test()
        {
            Assert.That(myself, Is.EqualTo(this));
            Assert.That(myotherself, Is.EqualTo(this));
        }
    }

    public class TestFieldInjection : TestFieldInjectionParent
    {
        [Dependency]
        private readonly TestFieldInjection myuniqueself;

        [Dependency]
        public TestFieldInjection mydifferentself;

        public override void Test()
        {
            base.Test();
            Assert.That(myuniqueself, Is.EqualTo(this));
            Assert.That(mydifferentself, Is.EqualTo(this));
        }
    }

    public class TestUnregisteredInjection
    {
        [Dependency]
        private readonly IIoCFailInterface FailInterface;
    }

    public class TestFailImplementation : IIoCFailInterface { }

    public interface ITestDisposeExceptionCaught { }

    public class TestDisposeExceptionCaught : ITestDisposeExceptionCaught, IDisposable
    {
        public void Dispose()
        {
            throw new Exception("UNIT TEST EXCEPTION");
        }
    }
}
