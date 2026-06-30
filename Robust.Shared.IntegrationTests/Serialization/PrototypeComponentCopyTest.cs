using NUnit.Framework;
using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
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

    [Test]
    public void CopyComponentFromPrototypeDoesNotValueCopyReadWriteOnlyCustomSerializerFields()
    {
        var serialization = IoCManager.Resolve<ISerializationManager>();

        IComponent source = new PrototypeCopyReadWriteOnlyCustomSerializerComponent
        {
            Value = new(3),
        };

        IComponent targetComponent = new PrototypeCopyReadWriteOnlyCustomSerializerComponent();

        EntityPrototype.CopyComponentFromPrototype(source, ref targetComponent, serialization);

        var target = (PrototypeCopyReadWriteOnlyCustomSerializerComponent) targetComponent;
        Assert.That(target.Value.Value, Is.EqualTo(4));
    }

    [Test]
    public void CopyComponentFromPrototypeCopiesSimpleCollections()
    {
        var serialization = IoCManager.Resolve<ISerializationManager>();

        var source = new PrototypeCopyCollectionComponent
        {
            List = [1, 2, 3],
            Set = [4, 5, 6],
            Dictionary = new() { { "a", 1 }, { "b", 2 } },
            Array = [7, 8, 9],
            ProtoSet = ["proto-a", "proto-b"],
            StructSet = [new("struct-a"), new("struct-b")],
            Nested = new()
            {
                Value = 10,
                Values = [11, 12],
            },
        };

        IComponent targetComponent = new PrototypeCopyCollectionComponent();

        EntityPrototype.CopyComponentFromPrototype(source, ref targetComponent, serialization);

        var target = (PrototypeCopyCollectionComponent) targetComponent;
        Assert.That(target.List, Is.EqualTo(source.List));
        Assert.That(target.Set, Is.EquivalentTo(source.Set));
        Assert.That(target.Dictionary, Is.EqualTo(source.Dictionary));
        Assert.That(target.Array, Is.EqualTo(source.Array));
        Assert.That(target.ProtoSet, Is.EquivalentTo(source.ProtoSet));
        Assert.That(target.StructSet, Is.EquivalentTo(source.StructSet));
        Assert.That(target.Nested.Value, Is.EqualTo(source.Nested.Value));
        Assert.That(target.Nested.Values, Is.EqualTo(source.Nested.Values));

        Assert.That(target.List, Is.Not.SameAs(source.List));
        Assert.That(target.Set, Is.Not.SameAs(source.Set));
        Assert.That(target.Dictionary, Is.Not.SameAs(source.Dictionary));
        Assert.That(target.Array, Is.Not.SameAs(source.Array));
        Assert.That(target.ProtoSet, Is.Not.SameAs(source.ProtoSet));
        Assert.That(target.StructSet, Is.Not.SameAs(source.StructSet));
        Assert.That(target.Nested, Is.Not.SameAs(source.Nested));
        Assert.That(target.Nested.Values, Is.Not.SameAs(source.Nested.Values));
    }

    [Test]
    public void CopyComponentFromPrototypeCopiesDirectCopyByValueStruct()
    {
        var serialization = IoCManager.Resolve<ISerializationManager>();

        IComponent source = new PrototypeCopyByValueComponent
        {
            Value = new("direct-value"),
        };

        IComponent targetComponent = new PrototypeCopyByValueComponent();

        EntityPrototype.CopyComponentFromPrototype(source, ref targetComponent, serialization);

        var target = (PrototypeCopyByValueComponent) targetComponent;
        Assert.That(target.Value, Is.EqualTo(new PrototypeCopyReadonlyStruct("direct-value")));
    }

    [Test]
    public void CopyComponentFromPrototypeCopiesCopyByRefReference()
    {
        var serialization = IoCManager.Resolve<ISerializationManager>();
        var value = new PrototypeCopyByRefData { Value = 5 };

        IComponent source = new PrototypeCopyByRefComponent
        {
            Value = value,
        };

        IComponent targetComponent = new PrototypeCopyByRefComponent();

        EntityPrototype.CopyComponentFromPrototype(source, ref targetComponent, serialization);

        var target = (PrototypeCopyByRefComponent) targetComponent;
        Assert.That(target.Value, Is.SameAs(value));
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

    [TypeSerializer]
    internal sealed class PrototypeCopyDefaultStructSerializer
        : ITypeSerializer<PrototypeCopyDefaultStruct, ValueDataNode>,
            ITypeCopyCreator<PrototypeCopyDefaultStruct>
    {
        public ValidationNode Validate(
            ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }

        public PrototypeCopyDefaultStruct Read(
            ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<PrototypeCopyDefaultStruct>? instanceProvider = null)
        {
            throw new NotImplementedException();
        }

        public DataNode Write(
            ISerializationManager serializationManager,
            PrototypeCopyDefaultStruct value,
            IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }

        public PrototypeCopyDefaultStruct CreateCopy(
            ISerializationManager serializationManager,
            PrototypeCopyDefaultStruct source,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            return source with { Value = source.Value + 1 };
        }
    }

    internal sealed class PrototypeCopyReadWriteOnlyStructSerializer
        : ITypeSerializer<PrototypeCopyDefaultStruct, ValueDataNode>
    {
        public ValidationNode Validate(
            ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }

        public PrototypeCopyDefaultStruct Read(
            ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<PrototypeCopyDefaultStruct>? instanceProvider = null)
        {
            throw new NotImplementedException();
        }

        public DataNode Write(
            ISerializationManager serializationManager,
            PrototypeCopyDefaultStruct value,
            IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            throw new NotImplementedException();
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

    [RegisterComponent]
    internal sealed partial class PrototypeCopyReadWriteOnlyCustomSerializerComponent : Component
    {
        [DataField(customTypeSerializer: typeof(PrototypeCopyReadWriteOnlyStructSerializer))]
        public PrototypeCopyDefaultStruct Value;
    }

    [RegisterComponent]
    internal sealed partial class PrototypeCopyCollectionComponent : Component
    {
        [DataField] public List<int> List = new();
        [DataField] public HashSet<int> Set = new();
        [DataField] public Dictionary<string, int> Dictionary = new();
        [DataField] public int[] Array = [];
        [DataField] public HashSet<ProtoId<EntityPrototype>> ProtoSet = new();
        [DataField] public HashSet<PrototypeCopyReadonlyStruct> StructSet = new();
        [DataField] public PrototypeCopyNestedData Nested = new();
    }

    [RegisterComponent]
    internal sealed partial class PrototypeCopyByValueComponent : Component
    {
        [DataField] public PrototypeCopyReadonlyStruct Value;
    }

    [RegisterComponent]
    internal sealed partial class PrototypeCopyByRefComponent : Component
    {
        [DataField] public PrototypeCopyByRefData Value = default!;
    }

    [CopyByValue]
    internal readonly record struct PrototypeCopyReadonlyStruct(string Id);

    [CopyByRef]
    internal sealed class PrototypeCopyByRefData
    {
        public int Value;
    }

    internal readonly record struct PrototypeCopyDefaultStruct(int Value);

    [DataDefinition]
    internal sealed partial class PrototypeCopyNestedData
    {
        [DataField] public int Value;
        [DataField] public List<int> Values = new();
    }
}
