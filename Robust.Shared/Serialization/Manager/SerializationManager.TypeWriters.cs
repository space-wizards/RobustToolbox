using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private bool TryWriteWithTypeSerializers(
            Type type,
            object obj,
            [NotNullWhen(true)] out DataNode? node,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            //TODO Paul: do this shit w/ delegates
            var method = typeof(SerializationManager).GetRuntimeMethods().First(m =>
                m.Name == nameof(TryWriteWithTypeSerializers) && m.GetParameters().Length == 4).MakeGenericMethod(type);

            node = null;

            var arr = new[] {obj, node, alwaysWrite, context};
            var res = method.Invoke(this, arr);

            if (res as bool? ?? false)
            {
                node = (DataNode) arr[1]!;
                return true;
            }

            return false;
        }

        private bool TryGetWriter<T>(ISerializationContext? context, [NotNullWhen(true)] out ITypeWriter<T>? writer) where T : notnull
        {
            if (context != null && context.TypeWriters.TryGetValue(typeof(T), out var rawTypeWriter) ||
                _typeWriters.TryGetValue(typeof(T), out rawTypeWriter))
            {
                writer = (ITypeWriter<T>) rawTypeWriter;
                return true;
            }

            return TryGetGenericWriter(out writer);
        }

        private bool TryWriteWithTypeSerializers<T>(
            T obj,
            [NotNullWhen(true)] out DataNode? node,
            bool alwaysWrite = false,
            ISerializationContext? context = null) where T : notnull
        {
            node = default;
            if (TryGetWriter<T>(context, out var writer))
            {
                node = writer.Write(this, obj, alwaysWrite, context);
                return true;
            }

            return false;
        }

        private bool TryGetGenericWriter<T>([NotNullWhen(true)] out ITypeWriter<T>? rawWriter) where T : notnull
        {
            rawWriter = null;

            if (typeof(T).IsGenericType)
            {
                var typeDef = typeof(T).GetGenericTypeDefinition();

                Type? serializerTypeDef = null;

                foreach (var (key, val) in _genericWriterTypes)
                {
                    if (typeDef.HasSameMetadataDefinitionAs(key))
                    {
                        serializerTypeDef = val;
                        break;
                    }
                }

                if (serializerTypeDef == null) return false;

                var serializerType = serializerTypeDef.MakeGenericType(typeof(T).GetGenericArguments());
                rawWriter = (ITypeWriter<T>) RegisterSerializer(serializerType)!;

                return true;
            }

            return false;
        }
    }
}
