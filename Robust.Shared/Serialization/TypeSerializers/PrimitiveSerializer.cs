using System.Globalization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class PrimitiveSerializer : ITypeSerializer<bool>, ITypeSerializer<byte>, ITypeSerializer<sbyte>, ITypeSerializer<char>, ITypeSerializer<decimal>, ITypeSerializer<double>, ITypeSerializer<float>, ITypeSerializer<int>, ITypeSerializer<uint>, ITypeSerializer<long>, ITypeSerializer<ulong>, ITypeSerializer<short>, ITypeSerializer<ushort>
    {
        bool ITypeSerializer<bool>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return bool.Parse(valueDataNode.GetValue());
        }

        public IDataNode TypeToNode(ushort value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString());
        }

        public IDataNode TypeToNode(short value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString());
        }

        public IDataNode TypeToNode(ulong value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString());
        }

        public IDataNode TypeToNode(long value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString());
        }

        public IDataNode TypeToNode(uint value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString());
        }

        public IDataNode TypeToNode(int value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString());
        }

        public IDataNode TypeToNode(float value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public IDataNode TypeToNode(double value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public IDataNode TypeToNode(decimal value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public IDataNode TypeToNode(char value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString());
        }

        public IDataNode TypeToNode(sbyte value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString());
        }

        public IDataNode TypeToNode(byte value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString());
        }

        public IDataNode TypeToNode(bool value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString());
        }

        byte ITypeSerializer<byte>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return byte.Parse(valueDataNode.GetValue());
        }

        sbyte ITypeSerializer<sbyte>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return sbyte.Parse(valueDataNode.GetValue());
        }

        char ITypeSerializer<char>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return char.Parse(valueDataNode.GetValue());        }

        decimal ITypeSerializer<decimal>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return decimal.Parse(valueDataNode.GetValue());
        }

        double ITypeSerializer<double>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return double.Parse(valueDataNode.GetValue());
        }

        float ITypeSerializer<float>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return float.Parse(valueDataNode.GetValue());
        }

        int ITypeSerializer<int>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return int.Parse(valueDataNode.GetValue());
        }

        uint ITypeSerializer<uint>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return uint.Parse(valueDataNode.GetValue());
        }

        long ITypeSerializer<long>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return long.Parse(valueDataNode.GetValue());
        }

        ulong ITypeSerializer<ulong>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return ulong.Parse(valueDataNode.GetValue());
        }

        short ITypeSerializer<short>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return short.Parse(valueDataNode.GetValue());
        }

        ushort ITypeSerializer<ushort>.NodeToType(IDataNode node, ISerializationContext? context)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return ushort.Parse(valueDataNode.GetValue());
        }
    }
}
