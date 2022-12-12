using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager;

public sealed partial class SerializationManager
{
    private delegate void CopyToBoxingDelegate(
            object source,
            ref object target,
            bool skipHook,
            ISerializationContext? context = null);

    private delegate bool CopyToGenericDelegate<T>(
        T source,
        ref T target,
        ISerializationContext? context = null,
        bool skipHook = false);

    private readonly ConcurrentDictionary<Type, object> _copyToGenericDelegates = new();
    private readonly ConcurrentDictionary<(Type baseType, Type actualType), object> _copyToGenericBaseDelegates = new();
    private readonly ConcurrentDictionary<Type, CopyToBoxingDelegate> _copyToBoxingDelegates = new();

    private CopyToBoxingDelegate GetOrCreateCopyToBoxingDelegate(Type commonType)
    {
        return _copyToBoxingDelegates.GetOrAdd(commonType, static (type, manager) =>
        {
            var managerConst = Expression.Constant(manager);
            var sourceParam = Expression.Parameter(typeof(object), "source");
            var targetParam = Expression.Parameter(typeof(object).MakeByRefType(), "target");
            var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
            var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

            var targetVar = Expression.Variable(type);

            var block = Expression.Block(
                new[] { targetVar },
                Expression.Assign(targetVar, Expression.Convert(targetParam, type)),
                Expression.Call(
                    managerConst,
                    nameof(CopyTo),
                    new[] { type },
                    Expression.Convert(sourceParam, type),
                    targetVar,
                    contextParam,
                    skipHookParam,
                    Expression.Constant(false)), //we already handled the override before calling this
                Expression.Assign(targetParam, Expression.Convert(targetVar, typeof(object))));

            return Expression.Lambda<CopyToBoxingDelegate>(
                block,
                sourceParam,
                targetParam,
                skipHookParam,
                contextParam).Compile();
        }, this);
    }

    private CopyToGenericDelegate<T> GetOrCreateCopyToGenericDelegate<T>(T source)
    {
        static object ValueFactory(Type baseType, Type actualType, SerializationManager manager)
        {
            var instanceParam = Expression.Constant(manager);
            var sourceParam = Expression.Parameter(baseType, "source");
            var targetParam = Expression.Parameter(baseType.MakeByRefType(), "target");
            var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
            var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

            Expression call;
            var sameType = baseType == actualType;

            var targetVar = sameType ? targetParam : Expression.Variable(actualType);
            Expression sourceVar = sameType ? sourceParam : Expression.Convert(sourceParam, actualType);
            if (manager._regularSerializerProvider.TryGetTypeSerializer(typeof(ITypeCopier<>), actualType, out var copier))
            {
                var copierType = typeof(ITypeCopier<>).MakeGenericType(actualType);
                var copierConstant = Expression.Constant(copier, copierType);

                call = Expression.Block(
                    Expression.Call(
                        instanceParam,
                        nameof(CopyTo),
                        new []{actualType},
                        copierConstant,
                        sourceVar,
                        targetVar,
                        contextParam,
                        skipHookParam,
                        Expression.Constant(false)),
                    Expression.Constant(true));
            }
            else
            {
                call = Expression.Call(instanceParam, nameof(CopyToInternal), new[] { actualType }, sourceVar, targetVar,
                    Expression.Constant(manager.GetDefinition(actualType), typeof(DataDefinition<>).MakeGenericType(actualType)),
                    instanceParam, skipHookParam, contextParam);
            }

            if (!sameType)
            {
                var returnVar = Expression.Variable(typeof(bool));
                call = Expression.Block(
                    new[] { targetVar, returnVar },
                    Expression.Assign(targetVar, Expression.Convert(targetParam, actualType)),
                    Expression.Assign(returnVar, call),
                    Expression.Assign(targetParam, targetVar),
                    returnVar);
            }

            return Expression.Lambda<CopyToGenericDelegate<T>>(call, sourceParam, targetParam, contextParam, skipHookParam).Compile();
        }

        var type = typeof(T);
        if (type.IsAbstract || type.IsInterface)
        {
            return (CopyToGenericDelegate<T>)_copyToGenericBaseDelegates.GetOrAdd((type, source!.GetType()),
                static (tuple, manager) => ValueFactory(tuple.baseType, tuple.actualType, manager), this);
        }

        return (CopyToGenericDelegate<T>) _copyToGenericDelegates
            .GetOrAdd(type, static (type, manager) => ValueFactory(type, type, manager), this);
    }

    private delegate object CreateCopyBoxingDelegate(
        object source,
        bool skipHook,
        ISerializationContext? context = null);

    private delegate T CreateCopyGenericDelegate<T>(
        T source,
        bool skipHook,
        ISerializationContext? context = null);

    private readonly ConcurrentDictionary<Type, object> _createCopyGenericDelegates = new();
    private readonly ConcurrentDictionary<Type, CreateCopyBoxingDelegate> _createCopyBoxingDelegates = new();

    private CreateCopyBoxingDelegate GetOrCreateCreateCopyBoxingDelegate(Type commonType)
    {
        return _createCopyBoxingDelegates.GetOrAdd(commonType, static (type, manager) =>
        {
            var managerConst = Expression.Constant(manager);
            var sourceParam = Expression.Parameter(typeof(object), "source");
            var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
            var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

            return Expression.Lambda<CreateCopyBoxingDelegate>(
                Expression.Convert(Expression.Call(
                    managerConst,
                    nameof(CreateCopy),
                    new[] { type },
                    Expression.Convert(sourceParam, type),
                    contextParam,
                    skipHookParam,
                    Expression.Constant(false)), typeof(object)),
                sourceParam,
                skipHookParam,
                contextParam).Compile();
        }, this);
    }

    private CreateCopyGenericDelegate<T> GetOrCreateCreateCopyGenericDelegate<T>()
    {
        return (CreateCopyGenericDelegate<T>)_createCopyGenericDelegates
            .GetOrAdd(typeof(T), static (type, manager) =>
            {
                var instanceParam = Expression.Constant(manager);
                var sourceParam = Expression.Parameter(type, "source");
                var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

                var actualType = type;
                type = type.EnsureNotNullableType(); //null values are already handled outside of the delegate
                var sourceParamAccess = Expression.Convert(sourceParam, type);
                Expression call;
                if (manager._regularSerializerProvider.TryGetTypeSerializer(typeof(ITypeCopyCreator<>), type, out var rawCopier))
                {
                    var serializerType = typeof(ITypeCopyCreator<>).MakeGenericType(type);
                    var copierConst = Expression.Constant(rawCopier, serializerType);
                    call = Expression.Call(
                        instanceParam,
                        nameof(CreateCopy),
                        new []{type},
                        copierConst,
                        sourceParamAccess,
                        contextParam,
                        skipHookParam,
                        Expression.Constant(false));
                }
                else if (type.IsArray)
                {
                    call = Expression.Call(
                        instanceParam,
                        nameof(CreateArrayCopy),
                        new[]{type.GetElementType()!},
                        sourceParamAccess,
                        contextParam,
                        skipHookParam);
                }
                else
                {
                    if (type.IsAbstract || type.IsInterface)
                    {
                        call = Expression.Convert(Expression.Call(
                            instanceParam,
                            nameof(CreateCopy),
                            Type.EmptyTypes,
                            Expression.Convert(sourceParam, typeof(object)),
                            contextParam,
                            skipHookParam,
                            Expression.Constant(false)), type);
                    }
                    else
                    {
                        call = Expression.Call(
                            instanceParam,
                            nameof(CreateCopyInternal),
                            new[] {type},
                            sourceParamAccess,
                            contextParam,
                            skipHookParam,
                            Expression.Constant(manager.GetDefinition(type), typeof(DataDefinition<>).MakeGenericType(type)));
                    }
                }

                return Expression.Lambda<CreateCopyGenericDelegate<T>>(
                    Expression.Convert(call, actualType),
                    sourceParam,
                    skipHookParam,
                    contextParam).Compile();
            }, this);
    }

    private bool ShouldReturnSource(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               _copyByRefRegistrations.Contains(type);
    }

    private bool CopyToInternal<TCommon>(
        TCommon source,
        ref TCommon target,
        DataDefinition<TCommon>? definition,
        ISerializationManager serializationManager,
        bool skipHook,
        ISerializationContext? context)
        where TCommon : notnull
    {
        if (context != null &&
            context.SerializerProvider.TryGetTypeSerializer<ITypeCopier<TCommon>, TCommon>(out var copier))
        {
            var commonTarget = target;
            copier.CopyTo(this, source, ref commonTarget, skipHook, context);
        }

        if (ShouldReturnSource(typeof(TCommon))) //todo paul can be precomputed
        {
            target = source;
            return true;
        }

        if (source is DataNode node)
        {
            target = (TCommon)(object)node.Copy();
            return true;
        }

        if (typeof(TCommon).IsArray) //todo paul can be precomputed
        {
            var sourceArray = (source as Array)!;
            var targetArray = (target as Array)!;

            Array newArray;
            if (sourceArray.Length == targetArray.Length)
            {
                newArray = targetArray;
            }
            else
            {
                newArray = (Array) Activator.CreateInstance(sourceArray.GetType(), sourceArray.Length)!;
            }

            for (var i = 0; i < sourceArray.Length; i++)
            {
                newArray.SetValue(CreateCopy(sourceArray.GetValue(i), context, skipHook), i);
            }

            //todo paul serv3 fix
            target = (TCommon)(object)newArray;
            return true;
        }

        //this check is in here on purpose. we cannot check this during expression tree generation due to the value maybe being handled by a custom typeserializer
        if (definition == null)
        {
            return false;
        }

        definition.CopyTo(source, ref target, context, skipHook);
        return true;
    }

    private T[] CreateArrayCopy<T>(T[] source, ISerializationContext context, bool skipHook)
    {
        var copy = new T[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            copy[i] = CreateCopy(source[i], context, skipHook);
        }

        return copy;
    }

    private T CreateCopyInternal<T>(T source, ISerializationContext context, bool skipHook, DataDefinition<T>? definition) where T : notnull
    {
        if (ShouldReturnSource(typeof(T)))
            return source;

        if (source is DataNode node)
            return (T)(object)node.Copy();

        var isRecord = definition?.IsRecord ?? false;
        var target = GetOrCreateInstantiator<T>(isRecord)();

        if (!GetOrCreateCopyToGenericDelegate<T>(source)(source, ref target, context, skipHook))
        {
            throw new CopyToFailedException<T>();
        }
        return target!;
    }

    private void NotNullOverrideCheck(bool notNullableOverride)
    {
        if (notNullableOverride) throw new NullNotAllowedException();
    }

    public void CopyTo(object? source, ref object? target, ISerializationContext? context = null, bool skipHook = false, bool notNullableOverride = false)
    {
        if (source == null)
        {
            NotNullOverrideCheck(notNullableOverride);
            target = null;
            return;
        }

        if (target == null)
        {
            target = CreateCopy(source, context, skipHook);
            return;
        }

        if (!TypeHelpers.TrySelectCommonType(source.GetType(), target.GetType(), out var commonType))
        {
            throw new InvalidOperationException($"Could not find common type in Copy for types {source.GetType()} and {target.GetType()}!");
        }

        GetOrCreateCopyToBoxingDelegate(commonType)(source, ref target, skipHook, context);
    }

    public void CopyTo<T>(T source, ref T? target, ISerializationContext? context = null, bool skipHook = false, bool notNullableOverride = false)
    {
        if (source == null)
        {
            NotNullOverrideCheck(notNullableOverride);
            target = default;
            return;
        }

        if (target == null)
        {
            target = CreateCopy(source, context, skipHook)!;
            return;
        }

        if (!GetOrCreateCopyToGenericDelegate<T>(source)(source, ref target, context))
        {
            target = CreateCopy(source, context);
        }

        if(!skipHook && target is ISerializationHooks hookres)
            hookres.AfterDeserialization();
    }

    public void CopyTo<T>(ITypeCopier<T> copier, T source, ref T? target, ISerializationContext? context = null,
        bool skipHook = false, bool notNullableOverride = false)
    {
        if (source == null)
        {
            NotNullOverrideCheck(notNullableOverride);
            target = default;
            return;
        }

        target ??= GetOrCreateInstantiator<T>(false)();

        copier.CopyTo(this, source, ref target, skipHook, context);

        if(!skipHook && target is ISerializationHooks hookres)
            hookres.AfterDeserialization();
    }

    public void CopyTo<T, TCopier>(T source, ref T? target, ISerializationContext? context = null, bool skipHook = false, bool notNullableOverride = false)
        where TCopier : ITypeCopier<T>
    {
        CopyTo(GetOrCreateCustomTypeSerializer<TCopier>(), source, ref target, context, skipHook, notNullableOverride);
    }

    public object? CreateCopy(object? source, ISerializationContext? context = null, bool skipHook = false, bool notNullableOverride = false)
    {
        if (source == null)
        {
            NotNullOverrideCheck(notNullableOverride);
            return null;
        }

        return GetOrCreateCreateCopyBoxingDelegate(source.GetType())(source, skipHook, context);
    }

    public T CreateCopy<T>(T source, ISerializationContext? context = null, bool skipHook = false, bool notNullableOverride = false)
    {
        if (source == null)
        {
            NotNullOverrideCheck(notNullableOverride);
            return default!;
        }

        var res = GetOrCreateCreateCopyGenericDelegate<T>()(source, skipHook, context);
        if (!skipHook && res is ISerializationHooks hooks)
        {
            hooks.AfterDeserialization();
        }

        return res;
    }

    public T CreateCopy<T>(ITypeCopyCreator<T> copyCreator, T source, ISerializationContext? context = null,
        bool skipHook = false, bool notNullableOverride = false)
    {
        if (source == null)
        {
            NotNullOverrideCheck(notNullableOverride);
            return default!;
        }

        var res = copyCreator.CreateCopy(this, source, skipHook, context);
        if (!skipHook && res is ISerializationHooks hooks)
        {
            hooks.AfterDeserialization();
        }

        return res;
    }

    public T CreateCopy<T, TCopyCreator>(T source, ISerializationContext? context = null, bool skipHook = false, bool notNullableOverride = false)
        where TCopyCreator : ITypeCopyCreator<T>
    {
        return CreateCopy(GetOrCreateCustomTypeSerializer<TCopyCreator>(), source, context, skipHook, notNullableOverride);
    }
}
