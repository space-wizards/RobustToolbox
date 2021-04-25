using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        public delegate bool CopyDelegate(
            object source,
            ref object target,
            bool skipHook,
            ISerializationContext? context = null);

        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, CopyDelegate>> _copyDelegates = new();

        private CopyDelegate GetOrCreateCopyDelegate(Type commonType, Type sourceType, Type targetType)
        {
            return _copyDelegates
                .GetOrAdd(commonType, _ => new ConcurrentDictionary<Type, CopyDelegate>())
                .GetOrAdd(commonType, (t, tuple) =>
                {
                    var instanceParam = Expression.Constant(this);
                    var sourceParam = Expression.Parameter(typeof(object), "source");
                    var targetParam = Expression.Parameter(typeof(object).MakeByRefType(), "target");
                    var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
                    var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

                    var call = Expression.Call(
                        instanceParam,
                        nameof(TryCopy),
                        new[] {t, tuple.sourceType, tuple.targetType},
                        Expression.Convert(sourceParam, tuple.sourceType),
                        Expression.Convert(targetParam, tuple.targetType),
                        skipHookParam,
                        contextParam);

                    return Expression.Lambda<CopyDelegate>(
                        call,
                        sourceParam,
                        targetParam,
                        skipHookParam,
                        contextParam).Compile();
                }, (sourceType, targetType));
        }

        private bool TryCopyRaw(Type type, object source, ref object target, bool skipHook, ISerializationContext? context = null)
        {
            return GetOrCreateCopyDelegate(type, source.GetType(), target.GetType())(source, ref target, skipHook, context);
        }

        private bool TryCopy<TCommon, TSource, TTarget>(
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
