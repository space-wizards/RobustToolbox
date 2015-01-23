using NetSerializer;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SS14.Shared.Serialization
{
    public class SS14Serializer
    {
        public static bool _initialized = false;

        public SS14Serializer()
        {
            if (!_initialized)
            {
                Type[] types = {};
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    types =
                        types.Concat(a.GetTypes().Where(t => typeof (INetSerializableType).IsAssignableFrom(t))).ToArray
                            ();
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