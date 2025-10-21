using System;
using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.UnitTesting.Shared.Serialization.SerializationTests;

/// <summary>
/// Class holding all the resources needed for <see cref="ManagerTests"/>. I opted to move them all in here because i wanted the TestFixture to not look so messy
/// </summary>
public sealed partial class ManagerTests : ISerializationContext
{
    private static ValueDataNode SerializerRanDataNode => new ("SerializerRan");
    private static ValueDataNode SerializerRanCustomDataNode => new ("SerializerRanCustom");

    public SerializationManager.SerializerProvider SerializerProvider { get; } = new();
    public bool WritingReadingPrototypes { get; }

    [OneTimeSetUp]
    public void SetupSerializerProvider()
    {
        SerializerProvider.RegisterSerializer<CustomTypeSerializerStruct>();
        SerializerProvider.RegisterSerializer<CustomTypeSerializerClass>();
    }

    #region TypeSerializers (Struct)

    private record struct SerializerStruct(string OneValue, List<int> TwoValue)
    {
        public static SerializerStruct SerializerReturn() => new ("testValue", new List<int> { 2, 3 });
        public static SerializerStruct SerializerReturnAlt() => new ("anotherTestValue", new List<int> { 3, 4 });
        public static SerializerStruct SerializerCustomReturn() => new ("testValueCustom", new List<int> { 6, 17 });
        public static SerializerStruct SerializerCustomReturnAlt() => new ("anotherTestValueCustom", new List<int> { 23, 66 });
    }

    [TypeSerializer]
    private sealed class RegularTypeSerializerStruct : ITypeSerializer<SerializerStruct, ValueDataNode>, ITypeCopier<SerializerStruct>, ITypeCopyCreator<SerializerStruct>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }

        public SerializerStruct Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<SerializerStruct>? instanceProvider = null)
        {
            Assert.That(node, Is.EqualTo(SerializerRanDataNode));
            return SerializerStruct.SerializerReturn();
        }

        public DataNode Write(ISerializationManager serializationManager, SerializerStruct value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            return SerializerRanDataNode;
        }

        public void CopyTo(ISerializationManager serializationManager, SerializerStruct source, ref SerializerStruct target,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            target.OneValue = source.OneValue;
            target.TwoValue = source.TwoValue;
        }

        public SerializerStruct CreateCopy(ISerializationManager serializationManager, SerializerStruct source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return new SerializerStruct(source.OneValue, source.TwoValue);
        }
    }

    private sealed class CustomTypeSerializerStruct : ITypeSerializer<SerializerStruct, ValueDataNode>, ITypeCopier<SerializerStruct>, ITypeCopyCreator<SerializerStruct>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }

        public SerializerStruct Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<SerializerStruct>? instanceProvider = null)
        {
            Assert.That(node, Is.EqualTo(SerializerRanCustomDataNode));
            return SerializerStruct.SerializerCustomReturn();
        }

        public DataNode Write(ISerializationManager serializationManager, SerializerStruct value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            return SerializerRanCustomDataNode;
        }

        public void CopyTo(ISerializationManager serializationManager, SerializerStruct source, ref SerializerStruct target,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            target.OneValue = source.OneValue;
            target.TwoValue = source.TwoValue;
        }

        public SerializerStruct CreateCopy(ISerializationManager serializationManager, SerializerStruct source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return new SerializerStruct(source.OneValue, source.TwoValue);
        }
    }

    #endregion

    #region TypeSerializers (Class)

    private record SerializerClass(string OneValue, List<int> TwoValue)
    {
        public static SerializerClass SerializerReturn() => new ("testValue", new List<int> { 2, 3 });
        public static SerializerClass SerializerReturnAlt() => new ("anotherTestValue", new List<int> { 3, 4 });
        public static SerializerClass SerializerCustomReturn() => new ("testValueCustom", new List<int> { 6, 17 });
        public static SerializerClass SerializerCustomReturnAlt() => new ("anotherTestValueCustom", new List<int> { 23, 66 });
    };

    [TypeSerializer]
    private sealed class RegularTypeSerializerClass : ITypeSerializer<SerializerClass, ValueDataNode>, ITypeCopyCreator<SerializerClass>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }

        public SerializerClass Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<SerializerClass>? instanceProvider = null)
        {
            Assert.That(node, Is.EqualTo(SerializerRanDataNode));
            return SerializerClass.SerializerReturn();
        }

        public DataNode Write(ISerializationManager serializationManager, SerializerClass value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            return SerializerRanDataNode;
        }

        public SerializerClass CreateCopy(ISerializationManager serializationManager, SerializerClass source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return new SerializerClass(source.OneValue, source.TwoValue);
        }
    }

    private sealed class CustomTypeSerializerClass : ITypeSerializer<SerializerClass, ValueDataNode>, ITypeCopyCreator<SerializerClass>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }

        public SerializerClass Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<SerializerClass>? instanceProvider = null)
        {
            Assert.That(node, Is.EqualTo(SerializerRanCustomDataNode));
            return SerializerClass.SerializerCustomReturn();
        }

        public DataNode Write(ISerializationManager serializationManager, SerializerClass value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            return SerializerRanCustomDataNode;
        }

        public SerializerClass CreateCopy(ISerializationManager serializationManager, SerializerClass source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return new SerializerClass(source.OneValue, source.TwoValue);
        }
    }

    #endregion

    #region Other TypeDefinitions

    [CopyByRef]
    private sealed class CopyByRefTestClass {}

    [CopyByRef]
    private struct CopyByRefTestStruct
    {
        public int ID;
    }

    private enum TestEnum
    {
        A,
        B,
        C
    }

    private struct SelfSerializeStruct : ISelfSerialize
    {
        public string TestValue;

        public void Deserialize(string value)
        {
            TestValue = value;
        }

        public string Serialize()
        {
            return TestValue;
        }
    }

    private sealed class SelfSerializeClass : ISelfSerialize
    {
        public string TestValue = string.Empty;

        public void Deserialize(string value)
        {
            TestValue = value;
        }

        public string Serialize()
        {
            return TestValue;
        }
    }

    private interface IDataDefBaseInterface{}

    [DataDefinition]
    private partial struct DataDefStruct : IDataDefBaseInterface
    {
        [DataField("one")] public string OneValue;

        [DataField("two")] public List<int> TwoValue;
    }

    [DataDefinition]
    private sealed partial class DataDefClass : IDataDefBaseInterface
    {
        [DataField("one")] public string OneValue = string.Empty;

        [DataField("two")] public List<int> TwoValue = new();
    }

    #endregion
}
