using System;
using System.Reflection;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;

namespace Robust.UnitTesting.Shared.Serialization
{
    public abstract class SerializationTest : RobustUnitTest
    {
        protected IReflectionManager Reflection => IoCManager.Resolve<IReflectionManager>();
        protected ISerializationManager Serialization => IoCManager.Resolve<ISerializationManager>();

        protected virtual Assembly[] Assemblies => Array.Empty<Assembly>();

        protected override Assembly[] GetContentAssemblies()
        {
            return Assemblies;
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Serialization.Initialize();
        }
    }
}
