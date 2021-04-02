using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private bool TryCopyWithTypeCopier(Type type, object source, ref object target, bool skipHook, ISerializationContext? context = null)
        {
            //TODO Paul: do this shit w/ delegates
            var method = typeof(SerializationManager).GetRuntimeMethods().First(m =>
                m.Name == nameof(TryCopyWithTypeCopier) && m.GetParameters().Length == 4).MakeGenericMethod(type, source.GetType(), target.GetType());

            var arr = new[] {source, target, skipHook, context};
            var res = method.Invoke(this, arr);

            if (res as bool? ?? false)
            {
                target = arr[1]!;
                return true;
            }

            return false;
        }

        private bool TryCopyWithTypeCopier<TCommon, TSource, TTarget>(
            TSource source,
            ref TTarget target,
            bool skipHook,
            ISerializationContext? context = null)
            where TSource : TCommon
            where TTarget : TCommon
            where TCommon : notnull
        {
            object? rawTypeCopier;

            if (context != null &&
                context.TypeCopiers.TryGetValue(typeof(TCommon), out rawTypeCopier) ||
                _typeCopiers.TryGetValue(typeof(TCommon), out rawTypeCopier))
            {
                var ser = (ITypeCopier<TCommon>) rawTypeCopier;
                target = (TTarget) ser.Copy(this, source, target, skipHook, context);
                return true;
            }

            if (TryGetGenericCopier(out ITypeCopier<TCommon>? genericTypeWriter))
            {
                target = (TTarget) genericTypeWriter.Copy(this, source, target, skipHook, context);
                return true;
            }

            return false;
        }

        private bool TryGetGenericCopier<T>([NotNullWhen(true)] out ITypeCopier<T>? rawCopier) where T : notnull
        {
            rawCopier = null;

            if (typeof(T).IsGenericType)
            {
                var typeDef = typeof(T).GetGenericTypeDefinition();

                Type? serializerTypeDef = null;

                foreach (var (key, val) in _genericCopierTypes)
                {
                    if (typeDef.HasSameMetadataDefinitionAs(key))
                    {
                        serializerTypeDef = val;
                        break;
                    }
                }

                if (serializerTypeDef == null) return false;

                var serializerType = serializerTypeDef.MakeGenericType(typeof(T).GetGenericArguments());
                rawCopier = (ITypeCopier<T>) RegisterSerializer(serializerType)!;

                return true;
            }

            return false;
        }
    }
}
