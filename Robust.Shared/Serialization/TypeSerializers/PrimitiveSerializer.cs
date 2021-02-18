using System.Globalization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class PrimitiveSerializer : ITypeSerializer<bool>, ITypeSerializer<byte>, ITypeSerializer<sbyte>, ITypeSerializer<char>, ITypeSerializer<decimal>, ITypeSerializer<double>, ITypeSerializer<float>, ITypeSerializer<int>, ITypeSerializer<uint>, ITypeSerializer<long>, ITypeSerializer<ulong>, ITypeSerializer<short>, ITypeSerializer<ushort>
    {
        bool ITypeSerializer<bool>.NodeToType(DataNode node, ISerializationContext? context)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return bool.Parse(valueDataNode.GetValue());
        }

        public DataNode TypeToNode(ushort value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode TypeToNode(short value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode TypeToNode(ulong value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode TypeToNode(long value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode TypeToNode(uint value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode TypeToNode(int value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode TypeToNode(float value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public DataNode TypeToNode(double value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public DataNode TypeToNode(decimal value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public DataNode TypeToNode(char value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode TypeToNode(sbyte value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode TypeToNode(byte value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode TypeToNode(bool value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        byte ITypeSerializer<byte>.NodeToType(DataNode node, ISerializationContext? context)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return byte.Parse(valueDataNode.GetValue());
        }

        sbyte ITypeSerializer<sbyte>.NodeToType(DataNode node, ISerializationContext? context)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return sbyte.Parse(valueDataNode.GetValue());
        }

        char ITypeSerializer<char>.NodeToType(DataNode node, ISerializationContext? context)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return char.Parse(valueDataNode.GetValue());        }

        decimal ITypeSerializer<decimal>.NodeToType(DataNode node, ISerializationContext? context)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return decimal.Parse(valueDataNode.GetValue());
        }

        double ITypeSerializer<double>.NodeToType(DataNode node, ISerializationContext? context)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return double.Parse(valueDataNode.GetValue());
        }

        float ITypeSerializer<float>.NodeToType(DataNode node, ISerializationContext? context)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return float.Parse(valueDataNode.GetValue());
        }

        int ITypeSerializer<int>.NodeToType(DataNode node, ISerializationContext? context)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return int.Parse(valueDataNode.GetValue());
        }

        uint ITypeSerializer<uint>.NodeToType(DataNode node, ISerializationContext? context)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return uint.Parse(valueDataNode.GetValue());
        }

        long ITypeSerializer<long>.NodeToType(DataNode node, ISerializationContext? context)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return long.Parse(valueDataNode.GetValue());
        }

        ulong ITypeSerializer<ulong>.NodeToType(DataNode node, ISerializationContext? context)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return ulong.Parse(valueDataNode.GetValue());
        }

        short ITypeSerializer<short>.NodeToType(DataNode node, ISerializationContext? context)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return short.Parse(valueDataNode.GetValue());
        }

        ushort ITypeSerializer<ushort>.NodeToType(DataNode node, ISerializationContext? context)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return ushort.Parse(valueDataNode.GetValue());
        }
    }
}
