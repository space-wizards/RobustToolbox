using NUnit.Framework;
using SS14.Shared.IoC;
using SS14.Shared.IoC.Exceptions;
using System;
using Moq;

namespace SS14.UnitTesting.Shared.IoC
{
    [TestFixture]
    [TestOf(typeof(IoCManager))]
    [Parallelizable]
    public class IoCManager_Test
    {
        [SetUp]
        public void TestSetup()
        {
            // Because IoCManager is a singleton, it must be cleared every
            // test so that each test is isolated.
            IoCManager.Clear();
        }
        
        [Test]
        public void IoCTestFieldInjection()
        {
            IoCManager.Register<TestFieldInjection, TestFieldInjection>();
            IoCManager.BuildGraph();
            var tester = IoCManager.Resolve<TestFieldInjection>();

            tester.Test();
        }
        
        [Test]
        public void IoCTestBasic()
        {
            IoCManager.Register<TestFieldInjection, TestFieldInjection>();
            IoCManager.BuildGraph();

            Assert.That(IoCManager.Resolve<TestFieldInjection>(), Is.Not.Null);

            Assert.That(IoCManager.ResolveType(typeof(TestFieldInjection)), Is.Not.Null);
        }
        
        [Test]
        public void IoCTestOverwrite()
        {
            IoCManager.Register<IIoCTestPriorities, IoCTestPriorities2>();
            IoCManager.Register<IIoCTestPriorities, IoCTestPriorities1>(true);
            IoCManager.BuildGraph();

            Assert.That(IoCManager.Resolve<IIoCTestPriorities>(), Is.TypeOf<IoCTestPriorities1>());
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

        [Test]
        public void IoCBasicRegisterInstance()
        {
            var obj = new Mock<IIoCTestPriorities>().Object;

            IoCManager.RegisterInstance<IIoCTestPriorities>(obj);

            Assert.That(IoCManager.Resolve<IIoCTestPriorities>(), Is.EqualTo(obj));
        }
    }

    public interface IIoCFailInterface { }

    public interface IIoCTestPriorities { }

    public class IoCTestPriorities1 : IIoCTestPriorities { }
    public class IoCTestPriorities2 : IIoCTestPriorities { }

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
