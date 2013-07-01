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
            // Serializer will be initialized when the apps start up
            _serializerInitialized = true;
        }

        public void Serialize(NetOutgoingMessage message)
        {
            if(!_serializerInitialized)
            {
                InitSerializer();
            }
            var ms = new MemoryStream();
            //Thank you NetSerializer
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

            //Thank you NetSerializer
            return (MarshalComponentParameter)Serializer.Deserialize(ms);
        }
    }
}
