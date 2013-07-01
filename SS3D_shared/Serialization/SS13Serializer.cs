using System;
using System.IO;
using System.Linq;
using NetSerializer;

namespace SS13_Shared.Serialization
{
    public class SS13Serializer
    {
        public static bool _initialized = false;
        public SS13Serializer()
        {
            if(!_initialized)
            {
                Type[] types = {};
                foreach(var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    types = types.Concat(a.GetTypes().Where(t => typeof(INetSerializableType).IsAssignableFrom(t))).ToArray();
                }

                Serializer.Initialize(types); 
            }
        }

        public void Serialize(Stream stream, object obj)
        {
            Serializer.Serialize(stream, obj);
        }
    }
}
