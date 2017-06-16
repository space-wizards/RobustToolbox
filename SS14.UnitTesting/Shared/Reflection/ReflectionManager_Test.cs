using System;
using System.Collections.Generic;
using NUnit.Framework;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Reflection;

namespace SS14.UnitTesting.Shared.Reflection
{
    [TestFixture]
    public class ReflectionManager_Test : SS14UnitTest
    {
        [Test]
        public void ReflectionManager_TestGetAllChildren()
        {
            IReflectionManager reflectionManager = IoCManager.Resolve<IReflectionManager>();

            // I have no idea how to better do this.
            bool did1 = false;
            bool did2 = false;
            foreach (var type in reflectionManager.GetAllChildren<IReflectionManagerTest>())
            {
                if (!did1 && type == typeof(ReflectionManagerTestClass1))
                {
                    did1 = true;
                }
                else if (!did2 && type == typeof(ReflectionManagerTestClass2))
                {
                    did2 = true;
                }
                else if (type == typeof(ReflectionManagerTestClass3))
                {
                    // Not possible since it has [Reflect(false)]
                    Assert.Fail("ReflectionManager returned the [Reflect(false)] class.");
                }
                else if (type == typeof(ReflectionManagerTestClass4))
                {
                    Assert.Fail("ReflectionManager returned the abstract class");
                }
                else
                {
                    Assert.Fail("ReflectionManager returned too many types.");
                }
            }
            Assert.That(did1 && did2, Is.True, "IoCManager did not return both expected types. First: {0}, Second: {1}", did1, did2);
        }
    }

    // It's probably not amazing that the entire reflection manager is loaded in...
    [IoCTarget(Priority = 5)]
    public sealed class ReflectionManagerTest : ReflectionManager
    {
        protected override IEnumerable<string> TypePrefixes => new[] { "", "SS14.UnitTesting", "SS14.Server", "SS14.Client", "SS14.Shared" };
    }

    public interface IReflectionManagerTest { }

    // These two pass like normal.
    public class ReflectionManagerTestClass1 : IReflectionManagerTest { }
    public class ReflectionManagerTestClass2 : IReflectionManagerTest { }


    // These two should both NOT be passed.
    [Reflect(false)]
    public class ReflectionManagerTestClass3 : IReflectionManagerTest { }
    public abstract class ReflectionManagerTestClass4 : IReflectionManagerTest { }
}