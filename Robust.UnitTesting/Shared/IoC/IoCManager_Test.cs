using System;
using Moq;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;

namespace Robust.UnitTesting.Shared.IoC
{
    /// <remarks>
    /// This fixture CAN NOT be parallelized, because <see cref="IoCManager"/> is a static singleton.
    /// </remarks>
    [TestFixture, TestOf(typeof(IoCManager))]
    public class IoCManager_Test
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            IoCManager.InitThread();
        }

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
        public void IoCTestConstructorInjection()
        {
            IoCManager.Register<TestFieldInjection, TestFieldInjection>();
            IoCManager.Register<TestConstructorInjection, TestConstructorInjection>();
            IoCManager.BuildGraph();

            var tester = IoCManager.Resolve<TestConstructorInjection>();

            Assert.That(tester.FieldInjection, Is.Not.Null);
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
        public void IoCRegisterFactory()
        {
            var newInstance = new TestFieldInjection();
            IoCManager.Register<TestFieldInjection, TestFieldInjection>(() => newInstance);

            IoCManager.BuildGraph(); // Actually calls the factory

            var result = IoCManager.Resolve<TestFieldInjection>();
            Assert.That(result, Is.EqualTo(newInstance));
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
        public void IoCBasicRegisterInstance()
        {
            var obj = new Mock<IIoCTestPriorities>().Object;

            IoCManager.RegisterInstance<IIoCTestPriorities>(obj);

            Assert.That(IoCManager.Resolve<IIoCTestPriorities>(), Is.EqualTo(obj));
        }

        private class DependencyA
        {
            [Dependency] public readonly DependencyB _depB = default!;
        }
        private class DependencyB
        {
            [Dependency] public readonly DependencyA _depA = default!;
        }

        [Test]
        public void IoCRegInstancesBeforeBuildGraph()
        {
            var instanceA = new DependencyA();
            IoCManager.RegisterInstance<DependencyA>(instanceA, deferInject: true);

            var instanceB = new DependencyB();
            IoCManager.RegisterInstance<DependencyB>(instanceB, deferInject: true);

            IoCManager.BuildGraph();
            
            var resolveA = IoCManager.Resolve<DependencyA>();
            var resolveB = IoCManager.Resolve<DependencyB>();

            Assert.That(instanceA, Is.EqualTo(resolveA));
            Assert.That(instanceB, Is.EqualTo(resolveB));
            Assert.That(resolveA._depB, Is.EqualTo(resolveB));
            Assert.That(resolveB._depA, Is.EqualTo(resolveA));
        }

        [Test]
        public void IoCRegInstanceBeforeBuildGraph()
        {
            IoCManager.Register<DependencyA, DependencyA>();

            var instanceB = new DependencyB();
            IoCManager.RegisterInstance<DependencyB>(instanceB, deferInject: true);

            IoCManager.BuildGraph();
            
            var resolveA = IoCManager.Resolve<DependencyA>();
            var resolveB = IoCManager.Resolve<DependencyB>();

            Assert.That(instanceB, Is.EqualTo(resolveB));
            Assert.That(resolveA._depB, Is.EqualTo(resolveB));
            Assert.That(resolveB._depA, Is.EqualTo(resolveA));
        }

        [Test]
        public void IoCRegInstanceDepBeforeBuildGraph()
        {
            var instanceB = new DependencyB();
            IoCManager.RegisterInstance<DependencyB>(instanceB, deferInject: true);

            IoCManager.Register<DependencyA, DependencyA>();

            IoCManager.BuildGraph();

            var resolveA = IoCManager.Resolve<DependencyA>();
            var resolveB = IoCManager.Resolve<DependencyB>();

            Assert.That(instanceB, Is.EqualTo(resolveB));
            Assert.That(resolveA._depB, Is.EqualTo(resolveB));
            Assert.That(resolveB._depA, Is.EqualTo(resolveA));
        }

        [Test]
        public void TestExplicitInjection()
        {
            // This will explicitly cause the DynamicMethod code path to be taken.
            // To ensure that works.
            var test = new ExplicitInjectionTest();
            IoCManager.InjectDependencies(test);
            Assert.That(test.DependencyCollection, Is.Not.Null);
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
#pragma warning disable 649
        private readonly TestFieldInjection myself = default!;

        [Dependency]
        public TestFieldInjection myotherself = default!;
#pragma warning restore 649

        public virtual void Test()
        {
            Assert.That(myself, Is.EqualTo(this));
            Assert.That(myotherself, Is.EqualTo(this));
        }
    }

    public class TestFieldInjection : TestFieldInjectionParent
    {
        [Dependency]
#pragma warning disable 649
        private readonly TestFieldInjection myuniqueself = default!;

        [Dependency]
        public TestFieldInjection mydifferentself = default!;
#pragma warning restore 649

        public override void Test()
        {
            base.Test();
            Assert.That(myuniqueself, Is.EqualTo(this));
            Assert.That(mydifferentself, Is.EqualTo(this));
        }
    }

    public class TestConstructorInjection
    {
        public TestFieldInjection FieldInjection { get; }

        public TestConstructorInjection(TestFieldInjection fieldInjection)
        {
            FieldInjection = fieldInjection;
        }
    }

    public class TestUnregisteredInjection
    {
        [Dependency]
#pragma warning disable 414
        private readonly IIoCFailInterface FailInterface = default!;
#pragma warning restore 414
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

    public class ExplicitInjectionTest
    {
        [Dependency] public readonly IDependencyCollection DependencyCollection = default!;
    }
}
