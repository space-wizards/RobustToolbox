using System;
using System.IO;
using Lidgren.Network;
using NetSerializer;
using SS13_Shared.Serialization;

namespace SS13_Shared.GO
{
    [Serializable]
    public class MarshalComponentParameter : INetSerializableType
    {
        private static bool _serializerInitialized;

        public MarshalComponentParameter(ComponentFamily family, ComponentParameter parameter)
        {
            Family = family;
            Parameter = parameter;
        }

        public MarshalComponentParameter()
        {
        }

        public ComponentFamily Family { get; set; }

        public ComponentParameter Parameter { get; set; }

        private static void InitSerializer()
        {
            // Serializer will be initialized when the apps start up
            _serializerInitialized = true;
        }

        public void Serialize(NetOutgoingMessage message)
        {
            if (!_serializerInitialized)
            {
                InitSerializer();
            }
            var ms = new MemoryStream();
            //Thank you NetSerializer
            Serializer.Serialize(ms, this);
            message.Write((int) ms.Length);
            message.Write(ms.ToArray());
        }

        public byte[] Serialize()
        {
            if (!_serializerInitialized)
            {
                InitSerializer();
            }
            var ms = new MemoryStream();
            Serializer.Serialize(ms, this);
            return ms.ToArray();
        }

        public static MarshalComponentParameter Deserialize(NetIncomingMessage message)
        {
            if (!_serializerInitialized)
            {
                InitSerializer();
            }
            int length = message.ReadInt32();
            byte[] bytes = message.ReadBytes(length);
            var ms = new MemoryStream(bytes);

            //Thank you NetSerializer
            return (MarshalComponentParameter) Serializer.Deserialize(ms);
        }

        public static MarshalComponentParameter Deserialize(byte[] bytes)
        {
            if (!_serializerInitialized)
            {
                InitSerializer();
            }
            var ms = new MemoryStream(bytes);
            return (MarshalComponentParameter) Serializer.Deserialize(ms);
        }
    }
}