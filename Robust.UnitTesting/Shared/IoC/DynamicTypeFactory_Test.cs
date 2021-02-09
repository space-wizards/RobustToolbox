using Moq;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

namespace Robust.UnitTesting.Shared.IoC
{
    [TestFixture, Parallelizable, TestOf(typeof(DynamicTypeFactory))]
    public class DynamicTypeFactoryTests
    {
        [Test]
        public void InjectDepsBeforeConstructor()
        {
            var fakeLoader = new Mock<IModLoader>().Object;
            var deps = new DependencyCollection();
            deps.RegisterInstance<IModLoader>(fakeLoader);
            deps.Register<IDynamicTypeFactoryInternal, DynamicTypeFactory>();
            deps.BuildGraph();
            var factory = deps.Resolve<IDynamicTypeFactoryInternal>();
            
            var manager = factory.CreateInstanceUnchecked<DummyManager>();

            Assert.That(manager.Result, Is.EqualTo(fakeLoader));
        }

        private class DummyManager
        {
            // Note that we don't assign this field, because the static constructor will
            // overwrite it when the constructor is called.
            // Look into RuntimeHelpers.RunClassConstructor
            [Dependency] private readonly IModLoader _modLoader;
            public IModLoader Result { get; }
            public DummyManager()
            {
                _modLoader ??= default!; // no ur nullable
                Result = _modLoader;
            }
        }
    }
}
