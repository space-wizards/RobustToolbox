using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Robust.UnitTesting.Shared.Reflection
{
    public sealed class ReflectionManagerTest : ReflectionManager
    {
        protected override IEnumerable<string> TypePrefixes => new[] { "", "Robust.UnitTesting.", "Robust.Server.", "Robust.Shared." };
    }

    [TestFixture]
    public sealed class ReflectionManager_Test : RobustUnitTest
    {
        protected override void OverrideIoC()
        {
            base.OverrideIoC();

            IoCManager.Register<IReflectionManager, ReflectionManagerTest>(overwrite: true);
        }

        [Test]
        public void ReflectionManager_TestGetAllChildren()
        {
            IReflectionManager reflectionManager = IoCManager.Resolve<IReflectionManager>();

            // I have no idea how to better do this.
            bool did1 = false;
            bool did2 = false;
            foreach (var type in reflectionManager.GetAllChildren<IReflectionManagerTest>())
            {
                if (!did1 && type == typeof(TestClass1))
                {
                    did1 = true;
                }
                else if (!did2 && type == typeof(TestClass2))
                {
                    did2 = true;
                }
                else if (type == typeof(TestClass3))
                {
                    // Not possible since it has [Reflect(false)]
                    Assert.Fail("ReflectionManager returned the [Reflect(false)] class.");
                }
                else if (type == typeof(TestClass4))
                {
                    Assert.Fail("ReflectionManager returned the abstract class");
                }
                else
                {
                    Assert.Fail("ReflectionManager returned too many types.");
                }
            }
            Assert.That(did1 && did2, Is.True, $"IoCManager did not return both expected types. First: {did1}, Second: {did2}");
        }

        public interface IReflectionManagerTest { }

        // These two pass like normal.
        public sealed class TestClass1 : IReflectionManagerTest { }
        public sealed class TestClass2 : IReflectionManagerTest { }

        // These two should both NOT be passed.
        [Reflect(false)]
        public sealed class TestClass3 : IReflectionManagerTest { }
        public abstract class TestClass4 : IReflectionManagerTest { }

        [Test]
        public void ReflectionManager_TestGetType()
        {
            IReflectionManager reflectionManager = IoCManager.Resolve<IReflectionManager>();
            Assert.Multiple(() =>
            {
                Assert.That(reflectionManager.GetType("Shared.Reflection.TestGetType1"), Is.EqualTo(typeof(TestGetType1)));
                Assert.That(reflectionManager.GetType("Shared.Reflection.TestGetType2"), Is.EqualTo(typeof(TestGetType2)));
                Assert.That(reflectionManager.GetType("Shared.Reflection.ITestGetType3"), Is.EqualTo(typeof(ITestGetType3)));
            });
        }

        [Test]
        public void ReflectionManager_TestTryParseEnumReference()
        {
            IReflectionManager reflectionManager = IoCManager.Resolve<IReflectionManager>();
            reflectionManager.TryParseEnumReference("enum.TestParseEnumReferenceType1.Value", out var out1);
            reflectionManager.TryParseEnumReference("enum.TestParseEnumReferenceType2.InnerValue", out var out2);
            reflectionManager.TryParseEnumReference("enum.TestParseEnumReferenceType3.OuterValue", out var out3);
            reflectionManager.TryParseEnumReference("enum.TestParseEnumReferenceTypeClass+TestParseEnumReferenceType2.InnerValue", out var out4);
            Assert.Multiple(() =>
            {
                Assert.That(out1, Is.EqualTo(TestParseEnumReferenceType1.Value));
                Assert.That(out2, Is.EqualTo(TestParseEnumReferenceTypeClass.TestParseEnumReferenceType2.InnerValue));
                Assert.That(out3, Is.EqualTo(TestParseEnumReferenceType3.OuterValue));
                Assert.That(out4, Is.EqualTo(TestParseEnumReferenceTypeClass.TestParseEnumReferenceType2.InnerValue));
            });
        }
    }

    public sealed class TestGetType1 { }
    public abstract class TestGetType2 { }
    public interface ITestGetType3 { }

    public enum TestParseEnumReferenceType1 { Value }

    [UsedImplicitly]
    public sealed class TestParseEnumReferenceTypeClass
    {
        public enum TestParseEnumReferenceType2 { InnerValue }
    }
}

public enum TestParseEnumReferenceType3 { OuterValue }
