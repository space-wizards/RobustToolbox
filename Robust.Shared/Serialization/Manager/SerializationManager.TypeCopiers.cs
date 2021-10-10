using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private delegate bool CopyDelegate(
            object source,
            ref object target,
            bool skipHook,
            ISerializationContext? context = null);

        private readonly Dictionary<Type, object> _typeCopiers = new();
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

                    var targetCastVariable = Expression.Variable(tuple.targetType, "targetCastVariable");

                    var returnVariable = Expression.Variable(typeof(bool), "return");

                    var call = Expression.Call(
                        instanceParam,
                        nameof(TryCopy),
                        new[] {t, tuple.sourceType, tuple.targetType},
                        Expression.Convert(sourceParam, tuple.sourceType),
                        targetCastVariable,
                        skipHookParam,
                        contextParam);

                    var block = Expression.Block(
                        new[] {targetCastVariable, returnVariable},
                        Expression.Assign(
                            targetCastVariable,
                            Expression.Convert(targetParam, tuple.targetType)),
                        Expression.Assign(returnVariable, call),
                        Expression.IfThen(
                            returnVariable,
                            Expression.Assign(targetParam, targetCastVariable)),
                        returnVariable);

                    return Expression.Lambda<CopyDelegate>(
                        block,
                        sourceParam,
                        targetParam,
                        skipHookParam,
                        contextParam).Compile();
                }, (sourceType, targetType));
        }

        private bool TryCopyRaw(
            Type type,
            object source,
            ref object target,
            bool skipHook,
            ISerializationContext? context = null)
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
            object? rawCopier;

            if (context != null &&
                context.TypeCopiers.TryGetValue(typeof(TCommon), out rawCopier) ||
                _typeCopiers.TryGetValue(typeof(TCommon), out rawCopier))
            {
                var copier = (ITypeCopier<TCommon>) rawCopier;
                target = (TTarget) copier.Copy(this, source, target, skipHook, context);
                return true;
            }

            if (TryGetGenericCopier(out ITypeCopier<TCommon>? genericCopier))
            {
                target = (TTarget) genericCopier.Copy(this, source, target, skipHook, context);
                return true;
            }

            return false;
        }

        private bool TryGetGenericCopier<T>([NotNullWhen(true)] out ITypeCopier<T>? rawCopier)
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
