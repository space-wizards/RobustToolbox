using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace SS13_Shared.Utility
{
    internal class NetParamsPacker
    {
        private byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;
            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        private Object ByteArrayToObject(byte[] bytes)
        {
            if (bytes.Length == 0)
                throw new SerializationException("Cannot deserialize empty byte array.");
            var ms = new MemoryStream(bytes);
            var bf = new BinaryFormatter();
            return bf.Deserialize(ms);
        }

        public object Unpack(byte[] bytes)
        {
            return ByteArrayToObject(bytes);
        }

        public int Pack(Object obj, out byte[] bytes)
        {
            bytes = ObjectToByteArray(obj);
            return bytes.Length;
        }
    }
}