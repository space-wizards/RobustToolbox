using NetSerializer;
using SFML.Graphics;
using SFML.System;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenTK;

namespace SS14.Shared.Serialization
{
    public class SS14Serializer : ISS14Serializer
    {
        [Dependency]
        private readonly IReflectionManager reflectionManager;
        private Serializer Serializer;

        public void Initialize()
        {
            var types = reflectionManager.GetAllChildren<INetSerializableType>();
            var settings = new Settings()
            {
                CustomTypeSerializers = new ITypeSerializer[] { new SfmlTypeSerializer() }
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
    public class SfmlTypeSerializer : IStaticTypeSerializer, ITypeSerializer
    {
        private static HashSet<Type> handledTypes = new HashSet<Type>
        {
            typeof(Vector2),
            typeof(Vector2i),
            typeof(Vector2u),
            typeof(Vector3),
            typeof(IntRect),
            typeof(FloatRect),
            typeof(Color),
            typeof(Box2)
        };

        public bool Handles(Type type) => handledTypes.Contains(type);
        public IEnumerable<Type> GetSubtypes(Type type) => Enumerable.Empty<Type>();

        public MethodInfo GetStaticWriter(Type type)
        {
            return typeof(SfmlTypeSerializer).GetMethod("Write", new Type[] { typeof(Stream), type });
        }

        public MethodInfo GetStaticReader(Type type)
        {
            return typeof(SfmlTypeSerializer).GetMethod("Read", new Type[] { typeof(Stream), type.MakeByRefType() });
        }

        #region Vector2

        public static void Write(Stream stream, Vector2 value)
        {
            stream.Write(BitConverter.GetBytes(value.X), 0, sizeof(float));
            stream.Write(BitConverter.GetBytes(value.Y), 0, sizeof(float));
        }
        public static void Read(Stream stream, out Vector2 value)
        {
            var buffer = new byte[sizeof(float)];

            stream.Read(buffer, 0, buffer.Length);
            var x = BitConverter.ToSingle(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var y = BitConverter.ToSingle(buffer, 0);

            value = new Vector2(x, y);
        }

        #endregion Vector2

        #region Vector2i

        public static void Write(Stream stream, Vector2i value)
        {
            stream.Write(BitConverter.GetBytes(value.X), 0, sizeof(int));
            stream.Write(BitConverter.GetBytes(value.Y), 0, sizeof(int));
        }
        public static void Read(Stream stream, out Vector2i value)
        {
            var buffer = new byte[sizeof(int)];

            stream.Read(buffer, 0, buffer.Length);
            var x = BitConverter.ToInt32(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var y = BitConverter.ToInt32(buffer, 0);

            value = new Vector2i(x, y);
        }

        #endregion Vector2i

        #region Vector2u

        public static void Write(Stream stream, Vector2u value)
        {
            stream.Write(BitConverter.GetBytes(value.X), 0, sizeof(uint));
            stream.Write(BitConverter.GetBytes(value.Y), 0, sizeof(uint));
        }
        public static void Read(Stream stream, out Vector2u value)
        {
            var buffer = new byte[sizeof(uint)];

            stream.Read(buffer, 0, buffer.Length);
            var x = BitConverter.ToUInt32(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var y = BitConverter.ToUInt32(buffer, 0);

            value = new Vector2u(x, y);
        }

        #endregion Vector2u

        #region Vector3

        public static void Write(Stream stream, Vector3 value)
        {
            stream.Write(BitConverter.GetBytes(value.X), 0, sizeof(float));
            stream.Write(BitConverter.GetBytes(value.Y), 0, sizeof(float));
            stream.Write(BitConverter.GetBytes(value.Z), 0, sizeof(float));
        }
        public static void Read(Stream stream, out Vector3 value)
        {
            var buffer = new byte[sizeof(float)];

            stream.Read(buffer, 0, buffer.Length);
            var x = BitConverter.ToSingle(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var y = BitConverter.ToSingle(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var z = BitConverter.ToSingle(buffer, 0);

            value = new Vector3(x, y, z);
        }

        #endregion Vector3

        #region IntRect

        public static void Write(Stream stream, IntRect value)
        {
            stream.Write(BitConverter.GetBytes(value.Left), 0, sizeof(int));
            stream.Write(BitConverter.GetBytes(value.Top), 0, sizeof(int));
            stream.Write(BitConverter.GetBytes(value.Width), 0, sizeof(int));
            stream.Write(BitConverter.GetBytes(value.Height), 0, sizeof(int));
        }
        public static void Read(Stream stream, out IntRect value)
        {
            var buffer = new byte[sizeof(int)];

            stream.Read(buffer, 0, buffer.Length);
            var left = BitConverter.ToInt32(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var top = BitConverter.ToInt32(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var width = BitConverter.ToInt32(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var height = BitConverter.ToInt32(buffer, 0);

            value = new IntRect(left, top, width, height);
        }

        #endregion IntRect

        #region FloatRect

        public static void Write(Stream stream, FloatRect value)
        {
            stream.Write(BitConverter.GetBytes(value.Left), 0, sizeof(float));
            stream.Write(BitConverter.GetBytes(value.Top), 0, sizeof(float));
            stream.Write(BitConverter.GetBytes(value.Width), 0, sizeof(float));
            stream.Write(BitConverter.GetBytes(value.Height), 0, sizeof(float));
        }
        public static void Read(Stream stream, out FloatRect value)
        {
            var buffer = new byte[sizeof(float)];

            stream.Read(buffer, 0, buffer.Length);
            var left = BitConverter.ToSingle(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var top = BitConverter.ToSingle(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var width = BitConverter.ToSingle(buffer, 0);
            stream.Read(buffer, 0, buffer.Length);
            var height = BitConverter.ToSingle(buffer, 0);

            value = new FloatRect(left, top, width, height);
        }

        #endregion FloatRect

        #region Color

        public static void Write(Stream stream, Color value)
        {
            stream.Write(BitConverter.GetBytes(value.R), 0, sizeof(byte));
            stream.Write(BitConverter.GetBytes(value.G), 0, sizeof(byte));
            stream.Write(BitConverter.GetBytes(value.B), 0, sizeof(byte));
            stream.Write(BitConverter.GetBytes(value.A), 0, sizeof(byte));
        }
        public static void Read(Stream stream, out Color value)
        {
            var r = (byte)stream.ReadByte();
            var g = (byte)stream.ReadByte();
            var b = (byte)stream.ReadByte();
            var a = (byte)stream.ReadByte();

            value = new Color(r, g, b, a);
        }

        #endregion Color

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
