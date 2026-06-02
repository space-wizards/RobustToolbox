using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.UnitTesting.Shared.Serialization;

internal sealed partial class PrototypeComponentCopyTest : OurRobustUnitTest
{
    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();
    }

    [Test]
    public void CopyComponentFromPrototypeRunsSerializationHooks()
    {
        var serialization = IoCManager.Resolve<ISerializationManager>();

        IComponent sourceComponent = new PrototypeCopyHookComponent { Value = 3 };
        IComponent targetComponent = new PrototypeCopyHookComponent { Value = 3 };

        EntityPrototype.CopyComponentFromPrototype(sourceComponent, ref targetComponent, serialization);

        var target = (PrototypeCopyHookComponent) targetComponent;
        Assert.That(target.Value, Is.EqualTo(4));
    }

    [Test]
    public void CopyComponentFromPrototypeCopiesCustomSerializerFields()
    {
        var serialization = IoCManager.Resolve<ISerializationManager>();

        IComponent source = new PrototypeCopyCustomSerializerComponent { Value = 3 };

        IComponent targetComponent = new PrototypeCopyCustomSerializerComponent();

        EntityPrototype.CopyComponentFromPrototype(source, ref targetComponent, serialization);

        var target = (PrototypeCopyCustomSerializerComponent) targetComponent;
        Assert.That(target.Value, Is.EqualTo(4));
    }

    internal sealed class PrototypeCopyIntSerializer : ITypeCopier<int>, ITypeCopyCreator<int>
    {
        public void CopyTo(
            ISerializationManager serializationManager,
            int source,
            ref int target,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            target = source + 1;
        }

        public int CreateCopy(
            ISerializationManager serializationManager,
            int source,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            return source + 1;
        }
    }

    [RegisterComponent]
    internal sealed partial class PrototypeCopyHookComponent : Component, ISerializationHooks
    {
        [DataField]
        public int Value;

        void ISerializationHooks.AfterDeserialization()
        {
            Value++;
        }
    }

    [RegisterComponent]
    internal sealed partial class PrototypeCopyCustomSerializerComponent : Component
    {
        [DataField(customTypeSerializer: typeof(PrototypeCopyIntSerializer))]
        public int Value;
    }
}
