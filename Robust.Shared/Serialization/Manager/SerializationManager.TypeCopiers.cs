using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private delegate bool CopyDelegate(
            object source,
            ref object target,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null);

        private readonly Dictionary<Type, object?> _typeCopiers = new();
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
                    var hookCtxParam = Expression.Parameter(typeof(SerializationHookContext), "hookCtx");
                    var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

                    var targetCastVariable = Expression.Variable(tuple.targetType, "targetCastVariable");

                    var returnVariable = Expression.Variable(typeof(bool), "return");

                    var call = Expression.Call(
                        instanceParam,
                        nameof(TryCopy),
                        new[] {t, tuple.sourceType, tuple.targetType},
                        Expression.Convert(sourceParam, tuple.sourceType),
                        targetCastVariable,
                        hookCtxParam,
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
                        hookCtxParam,
                        contextParam).Compile();
                }, (sourceType, targetType));
        }

        private bool TryCopyRaw(
            Type type,
            object source,
            ref object target,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            return GetOrCreateCopyDelegate(type, source.GetType(), target.GetType())(source, ref target, hookCtx, context);
        }

        private bool TryCopy<TCommon, TSource, TTarget>(
            TSource source,
            ref TTarget target,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
            where TSource : TCommon
            where TTarget : TCommon
            where TCommon : notnull
        {
            if (GetTypeCopier<TCommon>(context) is { } copier)
            {
                target = (TTarget) copier.Copy(this, source, target, hookCtx, context);
                return true;
            }

            return false;
        }

        private ITypeCopier<T>? GetTypeCopier<T>(ISerializationContext? context)
        {
            if (context != null && context.TypeCopiers.TryGetValue(typeof(T), out var rawCopier))
                return (ITypeCopier<T>?)rawCopier;

            using (_serializerLock.ReadGuard())
            {
                if (_typeCopiers.TryGetValue(typeof(T), out rawCopier))
                    return (ITypeCopier<T>?)rawCopier;
            }

            using (_serializerLock.WriteGuard())
            {
                // Check again, in case it got added after releasing the read lock.
                if (_typeCopiers.TryGetValue(typeof(T), out rawCopier))
                    return (ITypeCopier<T>?)rawCopier;

                if (TryGetGenericCopier(out ITypeCopier<T>? genericCopier))
                    return genericCopier;

                _typeCopiers.Add(typeof(T), null);
                return null;
            }
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
