using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.UnitTesting.Shared.Serialization.SerializationTests;

public sealed partial class DataDefinitionTests
{
    private const int SerializerReturnInt = 424;
    private static readonly DataDummyStruct SerializerReturnStruct = new (){ Value = "SerializerReturn" };
    private static readonly DataDummyClass SerializerReturnClass = new (){ Value = "SerializerReturn" };

    private static ValueDataNode SerializerValueDataNode => new ("serializerData");
    private static SequenceDataNode SerializerSequenceDataNode => new (SerializerValueDataNode);
    private static MappingDataNode SerializerMappingDataNode => new (){{"data", SerializerValueDataNode}};

    public sealed class DataDefinitionValueCustomTypeSerializer
        : ITypeSerializer<int, ValueDataNode>,
            ITypeSerializer<DataDummyStruct, ValueDataNode>,
            ITypeSerializer<DataDummyClass, ValueDataNode>,
            ITypeCopier<int>, ITypeCopyCreator<int>,
            ITypeCopier<DataDummyStruct>, ITypeCopyCreator<DataDummyStruct>,
            ITypeCopier<DataDummyClass>, ITypeCopyCreator<DataDummyClass>
    {
        ValidationNode ITypeValidator<int, ValueDataNode>.Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            throw new NotImplementedException();
        }

        ValidationNode ITypeValidator<DataDummyStruct, ValueDataNode>.Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            throw new NotImplementedException();
        }

        ValidationNode ITypeValidator<DataDummyClass, ValueDataNode>.Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            throw new NotImplementedException();
        }

        public int Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<int>? instanceProvider = null)
        {
            return SerializerReturnInt;
        }

        public DataNode Write(ISerializationManager serializationManager, int value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            return SerializerValueDataNode;
        }

        public DataDummyStruct Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<DataDummyStruct>? instanceProvider = null)
        {
            return SerializerReturnStruct;
        }

        public DataNode Write(ISerializationManager serializationManager, DataDummyStruct value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            return SerializerValueDataNode;
        }

        public DataDummyClass Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<DataDummyClass>? instanceProvider = null)
        {
            return SerializerReturnClass;
        }

        public DataNode Write(ISerializationManager serializationManager, DataDummyClass value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            return SerializerValueDataNode;
        }

        public void CopyTo(ISerializationManager serializationManager, int source, ref int target,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            target = SerializerReturnInt;
        }

        public int CreateCopy(ISerializationManager serializationManager, int source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return SerializerReturnInt;
        }

        public void CopyTo(ISerializationManager serializationManager, DataDummyStruct source, ref DataDummyStruct target,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            target = SerializerReturnStruct;
        }

        public DataDummyStruct CreateCopy(ISerializationManager serializationManager, DataDummyStruct source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return SerializerReturnStruct;
        }

        public void CopyTo(ISerializationManager serializationManager, DataDummyClass source, ref DataDummyClass target,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            target = SerializerReturnClass;
        }

        public DataDummyClass CreateCopy(ISerializationManager serializationManager, DataDummyClass source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return SerializerReturnClass;
        }
    }

    public sealed class DataDefinitionSequenceCustomTypeSerializer
        : ITypeSerializer<int, SequenceDataNode>,
            ITypeSerializer<DataDummyStruct, SequenceDataNode>,
            ITypeSerializer<DataDummyClass, SequenceDataNode>,
            ITypeCopier<int>, ITypeCopyCreator<int>,
            ITypeCopier<DataDummyStruct>, ITypeCopyCreator<DataDummyStruct>,
            ITypeCopier<DataDummyClass>, ITypeCopyCreator<DataDummyClass>
    {
        ValidationNode ITypeValidator<int, SequenceDataNode>.Validate(ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            throw new NotImplementedException();
        }

        ValidationNode ITypeValidator<DataDummyStruct, SequenceDataNode>.Validate(ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            throw new NotImplementedException();
        }

        ValidationNode ITypeValidator<DataDummyClass, SequenceDataNode>.Validate(ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            throw new NotImplementedException();
        }

        public int Read(ISerializationManager serializationManager, SequenceDataNode node, IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<int>? instanceProvider = null)
        {
            return SerializerReturnInt;
        }

        public DataNode Write(ISerializationManager serializationManager, int value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            return SerializerSequenceDataNode;
        }


        public DataDummyStruct Read(ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<DataDummyStruct>? instanceProvider = null)
        {
            return SerializerReturnStruct;
        }

        public DataNode Write(ISerializationManager serializationManager, DataDummyStruct value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            return SerializerSequenceDataNode;
        }

        public DataDummyClass Read(ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<DataDummyClass>? instanceProvider = null)
        {
            return SerializerReturnClass;
        }

        public DataNode Write(ISerializationManager serializationManager, DataDummyClass value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            return SerializerSequenceDataNode;
        }

        public void CopyTo(ISerializationManager serializationManager, int source, ref int target,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            target = SerializerReturnInt;
        }

        public int CreateCopy(ISerializationManager serializationManager, int source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return SerializerReturnInt;
        }

        public void CopyTo(ISerializationManager serializationManager, DataDummyStruct source, ref DataDummyStruct target,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            target = SerializerReturnStruct;
        }

        public DataDummyStruct CreateCopy(ISerializationManager serializationManager, DataDummyStruct source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return SerializerReturnStruct;
        }

        public void CopyTo(ISerializationManager serializationManager, DataDummyClass source, ref DataDummyClass target,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            target = SerializerReturnClass;
        }

        public DataDummyClass CreateCopy(ISerializationManager serializationManager, DataDummyClass source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return SerializerReturnClass;
        }
    }

    public sealed class DataDefinitionMappingCustomTypeSerializer
        : ITypeSerializer<int, MappingDataNode>,
            ITypeSerializer<DataDummyStruct, MappingDataNode>,
            ITypeSerializer<DataDummyClass, MappingDataNode>,
            ITypeCopier<int>, ITypeCopyCreator<int>,
            ITypeCopier<DataDummyStruct>, ITypeCopyCreator<DataDummyStruct>,
            ITypeCopier<DataDummyClass>, ITypeCopyCreator<DataDummyClass>
    {
        ValidationNode ITypeValidator<int, MappingDataNode>.Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            throw new NotImplementedException();
        }

        ValidationNode ITypeValidator<DataDummyStruct, MappingDataNode>.Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            throw new NotImplementedException();
        }

        ValidationNode ITypeValidator<DataDummyClass, MappingDataNode>.Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            throw new NotImplementedException();
        }

        public int Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<int>? instanceProvider = null)
        {
            return SerializerReturnInt;
        }

        public DataNode Write(ISerializationManager serializationManager, int value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            return SerializerMappingDataNode;
        }

        public DataDummyStruct Read(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<DataDummyStruct>? instanceProvider = null)
        {
            return SerializerReturnStruct;
        }

        public DataNode Write(ISerializationManager serializationManager, DataDummyStruct value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            return SerializerMappingDataNode;
        }

        public DataDummyClass Read(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<DataDummyClass>? instanceProvider = null)
        {
            return SerializerReturnClass;
        }

        public DataNode Write(ISerializationManager serializationManager, DataDummyClass value, IDependencyCollection dependencies,
            bool alwaysWrite = false, ISerializationContext? context = null)
        {
            return SerializerMappingDataNode;
        }

        public void CopyTo(ISerializationManager serializationManager, int source, ref int target,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            target = SerializerReturnInt;
        }

        public int CreateCopy(ISerializationManager serializationManager, int source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return SerializerReturnInt;
        }

        public void CopyTo(ISerializationManager serializationManager, DataDummyStruct source, ref DataDummyStruct target,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            target = SerializerReturnStruct;
        }

        public DataDummyStruct CreateCopy(ISerializationManager serializationManager, DataDummyStruct source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return SerializerReturnStruct;
        }

        public void CopyTo(ISerializationManager serializationManager, DataDummyClass source, ref DataDummyClass target,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            target = SerializerReturnClass;
        }

        public DataDummyClass CreateCopy(ISerializationManager serializationManager, DataDummyClass source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return SerializerReturnClass;
        }
    }

    [CopyByRef]
    public struct DataDummyStruct : ISelfSerialize, IEquatable<DataDummyStruct>
    {
        public string Value;
        public void Deserialize(string value)
        {
            Value = value;
        }

        public string Serialize()
        {
            return Value;
        }

        public bool Equals(DataDummyStruct other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is DataDummyStruct other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    [CopyByRef]
    public sealed class DataDummyClass : ISelfSerialize, IEquatable<DataDummyClass>
    {
        public string Value = string.Empty;
        public void Deserialize(string value)
        {
            Value = value;
        }

        public string Serialize()
        {
            return Value;
        }

        public bool Equals(DataDummyClass? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((DataDummyClass) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

}
