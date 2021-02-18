using System.Globalization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class PrimitiveSerializer :
        ITypeSerializer<bool, ValueDataNode>,
        ITypeSerializer<byte, ValueDataNode>,
        ITypeSerializer<sbyte, ValueDataNode>,
        ITypeSerializer<char, ValueDataNode>,
        ITypeSerializer<decimal, ValueDataNode>,
        ITypeSerializer<double, ValueDataNode>,
        ITypeSerializer<float, ValueDataNode>,
        ITypeSerializer<int, ValueDataNode>,
        ITypeSerializer<uint, ValueDataNode>,
        ITypeSerializer<long, ValueDataNode>,
        ITypeSerializer<ulong, ValueDataNode>,
        ITypeSerializer<short, ValueDataNode>,
        ITypeSerializer<ushort, ValueDataNode>
    {
        bool ITypeReader<bool, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
        {
            return bool.Parse(node.Value);
        }

        byte ITypeReader<byte, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
        {

            return byte.Parse(node.Value);
        }

        sbyte ITypeReader<sbyte, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
        {

            return sbyte.Parse(node.Value);
        }

        char ITypeReader<char, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
        {

            return char.Parse(node.Value);        }

        decimal ITypeReader<decimal, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
        {

            return decimal.Parse(node.Value);
        }

        double ITypeReader<double, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
        {
            return double.Parse(node.Value);
        }

        float ITypeReader<float, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
        {
            return float.Parse(node.Value);
        }

        int ITypeReader<int, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
        {
            return int.Parse(node.Value);
        }

        uint ITypeReader<uint, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
        {

            return uint.Parse(node.Value);
        }

        long ITypeReader<long, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
        {

            return long.Parse(node.Value);
        }

        ulong ITypeReader<ulong, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
        {

            return ulong.Parse(node.Value);
        }

        short ITypeReader<short, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
        {

            return short.Parse(node.Value);
        }

        ushort ITypeReader<ushort, ValueDataNode>.Read(ValueDataNode node, ISerializationContext? context)
        {

            return ushort.Parse(node.Value);
        }

        public DataNode Write(ushort value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode Write(short value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode Write(ulong value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode Write(long value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode Write(uint value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode Write(int value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode Write(float value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public DataNode Write(double value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public DataNode Write(decimal value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public DataNode Write(char value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode Write(sbyte value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode Write(byte value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        public DataNode Write(bool value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }
    }
}
