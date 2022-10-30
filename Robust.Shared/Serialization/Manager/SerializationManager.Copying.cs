using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager;

public sealed partial class SerializationManager
{
    public delegate void CopyToDelegate(
            object source,
            ref object target,
            bool skipHook,
            ISerializationContext? context = null);

    private delegate object CreateCopyDelegate(
        object source,
        bool skipHook,
        ISerializationContext? context = null);

    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, CopyToDelegate>> _copyToDelegates = new();
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, CreateCopyDelegate>> _createCopyDelegates = new();

    private CopyToDelegate GetOrCreateCopyToDelegate(Type commonType, Type sourceType, Type targetType)
    {
        return _copyToDelegates
            .GetOrAdd(commonType, _ => new ConcurrentDictionary<Type, CopyToDelegate>())
            .GetOrAdd(commonType, static (t, tuple) =>
            {
                var instanceParam = Expression.Constant(tuple.manager);
                var sourceParam = Expression.Parameter(typeof(object), "source");
                var targetParam = Expression.Parameter(typeof(object).MakeByRefType(), "target");
                var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

                Expression call;
                if (tuple.manager._regularSerializerProvider.TryGetTypeSerializer(typeof(ITypeCopier<>), t, out var copier))
                {
                    var copierConstant = Expression.Constant(copier, typeof(ITypeCopier<>).MakeGenericType(t));

                    call = Expression.Call(
                        copierConstant,
                        "CopyTo",
                        Type.EmptyTypes,
                        instanceParam,
                        sourceParam,
                        targetParam,
                        skipHookParam,
                        contextParam);
                }
                else
                {
                    tuple.manager.TryGetDefinition(t, out var dataDef);
                    var dataDefConst = Expression.Constant(dataDef, typeof(DataDefinition));

                    call = Expression.Call(
                        instanceParam,
                        nameof(CopyToInternal),
                        new[] { t },
                        Expression.Convert(sourceParam, t),
                        Expression.Convert(targetParam, t),
                        dataDefConst,
                        instanceParam,
                        skipHookParam,
                        contextParam);
                }

                return Expression.Lambda<CopyToDelegate>(
                    call,
                    sourceParam,
                    targetParam,
                    skipHookParam,
                    contextParam).Compile();
            }, (sourceType, targetType, manager: this));
    }

    private CreateCopyDelegate GetOrCreateCreateCopyDelegate(Type sourceType)
    {
        return _createCopyDelegates
            .GetOrAdd(sourceType, _ => new ConcurrentDictionary<Type, CreateCopyDelegate>())
            .GetOrAdd(sourceType, static (t, tuple) =>
            {
                var instanceParam = Expression.Constant(tuple.manager);
                var sourceParam = Expression.Parameter(typeof(object), "source");
                var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

                Expression call;
                if (tuple.manager._regularSerializerProvider.TryGetTypeSerializer(typeof(ITypeCopyCreator<>), t, out var rawCopier))
                {
                    var copierConst = Expression.Constant(rawCopier, typeof(ITypeCopyCreator<>).MakeGenericType(t));
                    call = Expression.Call(
                        copierConst,
                        "CopyTo",
                        Array.Empty<Type>(),
                        instanceParam,
                        sourceParam,
                        skipHookParam,
                        contextParam);
                }
                else
                {
                    call = Expression.Call(
                        instanceParam,
                        nameof(CreateCopyInternal),
                        new[] {t},
                        sourceParam,
                        contextParam,
                        skipHookParam);
                }

                return Expression.Lambda<CreateCopyDelegate>(
                    call,
                    sourceParam,
                    skipHookParam,
                    contextParam).Compile();
            }, (sourceType, manager: this));
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
            if(sourceArray.Length == targetArray.Length)
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
            throw new ArgumentException($"No data definition found for type {typeof(TCommon)} with node type when running copyto");
        }

        var targetObj = (object)target!;
        definition.CopyTo(source!, ref targetObj, serializationManager, context);
    }

    //todo paul serv3 make common type checking more sane
    public void CopyTo(object source, ref object? target, ISerializationContext? context = null, bool skipHook = false)
    {
        if (target == null)
        {
            target = CreateCopy(source, context, skipHook);
            return;
        }

        if (!TypeHelpers.TrySelectCommonType(source.GetType(), target.GetType(), out var commonType))
        {
            throw new InvalidOperationException($"Could not find common type in Copy for types {source.GetType()} and {target.GetType()}!");
        }

        GetOrCreateCopyToDelegate(commonType, source.GetType(), target.GetType())(source, ref target, skipHook,
            context);

        if (!skipHook && target is ISerializationHooks afterHooks)
        {
            afterHooks.AfterDeserialization();
        }
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

        var tempCast = (object?)target;
        CopyTo(source, ref tempCast, context, skipHook);
        target = (T) tempCast!;
    }

    private T CreateCopyInternal<T>(T source, ISerializationContext context, bool skipHook)
    {
        //todo paul serv3 more?
        if (ShouldReturnSource(typeof(T)))
        {
            return source;
        }

        var target = (T)Activator.CreateInstance(typeof(T))!;

        CopyTo(source, ref target, context, skipHook);
        return target!;
    }

    public object? CreateCopy(object? source, ISerializationContext? context = null, bool skipHook = false)
    {
        if (source == null) return null;

        return GetOrCreateCreateCopyDelegate(source.GetType())(source, skipHook, context);
    }

    public T CreateCopy<T>(T source, ISerializationContext? context = null, bool skipHook = false)
    {
        return (T)CreateCopy((object?)source, context, skipHook)!;
    }
}
