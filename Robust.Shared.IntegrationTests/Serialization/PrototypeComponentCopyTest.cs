using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.UnitTesting.Shared.Serialization;

internal sealed class PrototypeComponentCopyTest : OurRobustUnitTest
{
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

    internal sealed class PrototypeCopyIntSerializer : ITypeCopyCreator<int>
    {
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
         [DataField(customTypeSerializer: typeof(PrototypeComponentCopyTest.PrototypeCopyIntSerializer))]
         public int Value;
     }
}

