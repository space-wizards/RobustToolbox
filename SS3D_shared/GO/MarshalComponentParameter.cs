using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Lidgren.Network;
using NetSerializer;

namespace SS13_Shared.GO
{
    [Serializable]
    public class MarshalComponentParameter
    {
        public ComponentFamily Family { get; set; }

        public ComponentParameter Parameter { get; set; }

        public MarshalComponentParameter(ComponentFamily family, ComponentParameter parameter)
        {
            Family = family;
            Parameter = parameter;
        }

        public MarshalComponentParameter()
        {}

        static bool _serializerInitialized = false;

        static void InitSerializer()
        {
            Type[] types = { typeof(MarshalComponentParameter) };
            Serializer.Initialize(types);
            _serializerInitialized = true;
        }

        public void Serialize(NetOutgoingMessage message)
        {
            if(!_serializerInitialized)
            {
                InitSerializer();
            }
            /*var packer = new Utility.NetParamsPacker();
            byte[] bytes;
            var length = (Int16)packer.Pack(this, out bytes);
            message.Write(length);
            message.Write(bytes);*/
            var ms = new MemoryStream();
            /*var bs = new BinaryFormatter();
            bs.Serialize(ms, this);*/
            Serializer.Serialize(ms, this);
            message.Write((int)ms.Length);
            message.Write(ms.ToArray());
        }

        public static MarshalComponentParameter Deserialize(NetIncomingMessage message)
        {
            if (!_serializerInitialized)
            {
                InitSerializer();
            }
            var length = message.ReadInt32();
            var bytes = message.ReadBytes(length);
            var ms = new MemoryStream(bytes);
            /*var bs = new BinaryFormatter();
            return (MarshalComponentParameter) bs.Deserialize(ms);*/
            return (MarshalComponentParameter)Serializer.Deserialize(ms);
            /*
            var packer = new Utility.NetParamsPacker();
            return (MarshalComponentParameter) packer.Unpack(bytes);*/
        }
    }
}
