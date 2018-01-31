using NetSerializer;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SS14.Shared.Maths;

namespace SS14.Shared.Serialization
{
    public class SS14Serializer : ISS14Serializer
    {
        [Dependency]
        private readonly IReflectionManager reflectionManager;
        private Serializer Serializer;

        public void Initialize()
        {
            var types = reflectionManager.FindTypesWithAttribute<NetSerializableAttribute>();
            var settings = new Settings()
            {
                CustomTypeSerializers = new ITypeSerializer[] { new OpenTKTypeSerializer() }
            };
            Serializer = new Serializer(types, settings);
        }

        public void Serialize(Stream stream, object toSerialize)
        {
            Serializer.Serialize(stream, toSerialize);
        }

        public T Deserialize<T>(Stream stream)
        {
            return (T)Deserialize(stream);
        }

        public object Deserialize(Stream stream)
        {
            return Serializer.Deserialize(stream);
        }
    }

    // Do all the dang work ourselves because they can't be bothered to put [Serializable] on their structs.
    public class OpenTKTypeSerializer : IStaticTypeSerializer, ITypeSerializer
    {
        private static HashSet<Type> handledTypes = new HashSet<Type>
        {
            typeof(Box2)
        };

        public bool Handles(Type type) => handledTypes.Contains(type);
        public IEnumerable<Type> GetSubtypes(Type type) => Enumerable.Empty<Type>();

        public MethodInfo GetStaticWriter(Type type)
        {
            return typeof(OpenTKTypeSerializer).GetMethod("Write", new Type[] { typeof(Stream), type });
        }

        public MethodInfo GetStaticReader(Type type)
        {
            return typeof(OpenTKTypeSerializer).GetMethod("Read", new Type[] { typeof(Stream), type.MakeByRefType() });
        }

        #region Box2

        public static void Write(Stream stream, Box2 value)
        {
            stream.Write(BitConverter.GetBytes(value.Left), 0, sizeof(float));
            stream.Write(BitConverter.GetBytes(value.Right), 0, sizeof(float));
            stream.Write(BitConverter.GetBytes(value.Top), 0, sizeof(float));
            stream.Write(BitConverter.GetBytes(value.Bottom), 0, sizeof(float));
        }

        public static void Read(Stream stream, out Box2 value)
        {
            var buffer = new byte[sizeof(float)];

            stream.Read(buffer, 0, buffer.Length);
            var left = BitConverter.ToSingle(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var right = BitConverter.ToSingle(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var top = BitConverter.ToSingle(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var bottom = BitConverter.ToSingle(buffer, 0);

            value = new Box2(left, top, right, bottom);
        }
        #endregion
    }

}
