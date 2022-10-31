using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Manager.Definition;
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

    private delegate void CopyToGenericDelegate<T>(
        T source,
        ref T target,
        ISerializationContext? context = null,
        bool skipHook = false);

    private readonly ConcurrentDictionary<Type, object> _copyToGenericDelegates = new();
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
                    skipHookParam),
                Expression.Assign(targetParam, Expression.Convert(targetVar, typeof(object))));

            return Expression.Lambda<CopyToBoxingDelegate>(
                block,
                sourceParam,
                targetParam,
                skipHookParam,
                contextParam).Compile();
        }, this);
    }

    private CopyToGenericDelegate<T> GetOrCreateCopyToGenericDelegate<T>()
    {
        return (CopyToGenericDelegate<T>) _copyToGenericDelegates
            .GetOrAdd(typeof(T), static (t, manager) =>
            {
                var instanceParam = Expression.Constant(manager);
                var sourceParam = Expression.Parameter(t, "source");
                var targetParam = Expression.Parameter(t.MakeByRefType(), "target");
                var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

                Expression call;

                if (manager._regularSerializerProvider.TryGetTypeSerializer(typeof(ITypeCopier<>), t, out var copier))
                {
                    var copierConstant = Expression.Constant(copier, typeof(ITypeCopier<>).MakeGenericType(t));

                    call = Expression.Call(
                        copierConstant,
                        typeof(ITypeCopier<>).MakeGenericType(t).GetMethod("CopyTo")!,
                        instanceParam,
                        sourceParam,
                        targetParam,
                        skipHookParam,
                        contextParam);
                }
                else
                {
                    var dataDefConst = Expression.Constant(manager.GetDefinition(t), typeof(DataDefinition));

                    call = Expression.Call(
                        instanceParam,
                        nameof(CopyToInternal),
                        new[] { t },
                        sourceParam,
                        targetParam,
                        dataDefConst,
                        instanceParam,
                        skipHookParam,
                        contextParam);
                }

                return Expression.Lambda<CopyToGenericDelegate<T>>(
                    call,
                    sourceParam,
                    targetParam,
                    contextParam,
                    skipHookParam).Compile();
            }, this);
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
                    skipHookParam), typeof(object)),
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

                Expression call;
                if (manager._regularSerializerProvider.TryGetTypeSerializer(typeof(ITypeCopyCreator<>), type, out var rawCopier))
                {
                    var serializerType = typeof(ITypeCopyCreator<>).MakeGenericType(type);
                    var copierConst = Expression.Constant(rawCopier, serializerType);
                    call = Expression.Call(
                        copierConst,
                        typeof(ITypeCopyCreator<>).MakeGenericType(type).GetMethod("CreateCopy")!,
                        instanceParam,
                        sourceParam,
                        skipHookParam,
                        contextParam);
                }
                else
                {
                    var dataDefConst = Expression.Constant(manager.GetDefinition(type), typeof(DataDefinition));
                    call = Expression.Call(
                        instanceParam,
                        nameof(CreateCopyInternal),
                        new[] {type},
                        sourceParam,
                        contextParam,
                        skipHookParam,
                        dataDefConst);
                }

                return Expression.Lambda<CreateCopyGenericDelegate<T>>(
                    call,
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
               _copyByRefRegistrations.Contains(type) ||
               type.IsValueType;
    }

    private void CopyToInternal<TCommon>(
        TCommon source,
        ref TCommon target,
        DataDefinition? definition,
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

        if (ShouldReturnSource(typeof(TCommon)))
        {
            target = source;
            return;
        }

        if (typeof(TCommon).IsArray)
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
            return;
        }

        if (definition == null)
        {
            if(!TryGetDefinition(source.GetType(), out definition))
                throw new ArgumentException($"No data definition found for type {typeof(TCommon)} when running CopyTo");
        }

        var targetObj = (object)target!;
        definition.CopyTo(source!, ref targetObj, serializationManager, context);
    }

    private T CreateCopyInternal<T>(T source, ISerializationContext context, bool skipHook, DataDefinition? definition) where T : notnull
    {
        Type type;
        if (typeof(T).IsAbstract || typeof(T).IsInterface)
        {
            type = source.GetType();
            definition ??= GetDefinition(source.GetType());
        }
        else
        {
            type = typeof(T);
        }

        if (ShouldReturnSource(type))
            return source;

        var isRecord = definition?.IsRecord ?? false;
        var target = (T) GetOrCreateInstantiator(type, isRecord)();

        CopyTo(source, ref target, context, skipHook);
        return target!;
    }

    public void CopyTo(object? source, ref object? target, ISerializationContext? context = null, bool skipHook = false)
    {
        if (source == null)
        {
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

    public void CopyTo<T>(T source, ref T? target, ISerializationContext? context = null, bool skipHook = false)
    {
        if (source == null)
        {
            target = default;
            return;
        }

        if (target == null)
        {
            target = CreateCopy(source, context, skipHook);
            return;
        }

        GetOrCreateCopyToGenericDelegate<T>()(source, ref target, context, skipHook);
    }

    public object? CreateCopy(object? source, ISerializationContext? context = null, bool skipHook = false)
    {
        if (source == null)
            return null;

        return GetOrCreateCreateCopyBoxingDelegate(source.GetType())(source, skipHook, context);
    }

    public T CreateCopy<T>(T source, ISerializationContext? context = null, bool skipHook = false)
    {
        if (source == null) return default!;

        return GetOrCreateCreateCopyGenericDelegate<T>()(source, skipHook, context);
    }
}
