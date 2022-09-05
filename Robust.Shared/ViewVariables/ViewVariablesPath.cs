using System;
using System.Linq;
using System.Reflection;
using Robust.Shared.Reflection;

namespace Robust.Shared.ViewVariables;

[Virtual]
public abstract class ViewVariablesPath
{
    public abstract Type Type { get; }
    public abstract object? Get();
    public abstract void Set(object? value);
    public abstract object? Invoke(object?[]? parameters);
    public virtual Type[] InvokeParameterTypes { get; } = Array.Empty<Type>();
    public virtual uint InvokeOptionalParameters { get; } = 0;
    public virtual Type InvokeReturnType { get; } = typeof(void);
}

public sealed class ViewVariablesFieldOrPropertyPath : ViewVariablesPath
{
    internal ViewVariablesFieldOrPropertyPath(object? obj, MemberInfo member)
    {
        if (member is not (FieldInfo or PropertyInfo))
            throw new ArgumentException("Member must be either a field or a property!", nameof(member));

        Object = obj;
        Member = member;
        ViewVariablesUtility.TryGetViewVariablesAccess(member, out Access);
    }

    public readonly object? Object;
    public readonly MemberInfo Member;
    public readonly VVAccess? Access;
    public override Type Type => Member.GetUnderlyingType();

    public override object? Get()
    {
        if (Access == null)
            return null;

        try
        {
            return Object != null
                ? Member.GetValue(Object)
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override void Set(object? value)
    {
        if (Access != VVAccess.ReadWrite)
            return;

        if (Object != null)
            Member.SetValue(Object, value);
    }

    public override object? Invoke(object?[]? parameters) => null;
}

public sealed class ViewVariablesMethodPath : ViewVariablesPath
{
    internal ViewVariablesMethodPath(object? obj, MethodInfo method)
    {
        Object = obj;
        Method = method;
        ViewVariablesUtility.TryGetViewVariablesAccess(method, out Access);
    }

    public readonly object? Object;
    public readonly MethodInfo Method;
    public readonly VVAccess? Access;
    public override Type Type => typeof(void);
    public override Type InvokeReturnType => Method.ReturnType;

    public override object? Get() => null;

    public override void Set(object? value)
    {
    }

    public override object? Invoke(object?[]? parameters)
    {
        if (Access != VVAccess.ReadWrite)
            return null;

        return Object != null
            ? Method.Invoke(Object, parameters)
            : null;
    }

    public override Type[] InvokeParameterTypes
        => Access == VVAccess.ReadWrite
            ? Method.GetParameters().Select(info => info.ParameterType).ToArray()
            : Array.Empty<Type>();
    public override uint InvokeOptionalParameters
        => Access == VVAccess.ReadWrite
            ? (uint) Method.GetParameters().Count(info => info.IsOptional)
            : 0;
}

public sealed class ViewVariablesIndexedPath : ViewVariablesPath
{
    internal ViewVariablesIndexedPath(object? obj, PropertyInfo indexer, object?[] index, VVAccess? parentAccess)
    {
        if (indexer.GetIndexParameters().Length == 0)
            throw new ArgumentException("PropertyInfo is not an indexer!", nameof(indexer));

        Object = obj;
        Indexer = indexer;
        Index = index;
        Access = parentAccess;
    }

    public readonly object? Object;
    public readonly PropertyInfo Indexer;
    public readonly object?[] Index;
    public readonly VVAccess? Access;
    public override Type Type => Indexer.GetUnderlyingType();

    public override object? Get()
    {
        if (Access == null)
            return null;

        try
        {
            return Object != null
                ? Indexer.GetValue(Object, Index)
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override void Set(object? value)
    {
        if(Access == VVAccess.ReadWrite && Object != null)
            Indexer.SetValue(Object, value, Index);
    }

    public override object? Invoke(object?[]? parameters) => null;
}

public sealed class ViewVariablesInstancePath : ViewVariablesPath
{
    public ViewVariablesInstancePath(object? obj)
    {
        Object = obj;
    }

    public readonly object? Object;

    public override Type Type => Object?.GetType() ?? typeof(void);

    public override object? Get() => Object;

    public override void Set(object? value)
    {
    }

    public override object? Invoke(object?[]? parameters) => null;
}

public sealed class ViewVariablesFakePath : ViewVariablesPath
{
    public ViewVariablesFakePath(Func<object?>? getter, Action<object?>? setter, Func<object?, object?>? invoker,
        Type type, Type[]? invokeParameterTypes = null, uint invokeOptionalParameters = 0, Type? invokeReturnType = null)
    {
        Getter = getter;
        Setter = setter;
        Invoker = invoker;
        Type = type;
        InvokeParameterTypes = invokeParameterTypes ?? Array.Empty<Type>();
        InvokeOptionalParameters = invokeOptionalParameters;
        InvokeReturnType = invokeReturnType ?? typeof(void);
    }

    public readonly Func<object?>? Getter;
    public readonly Action<object?>? Setter;
    public readonly Func<object?, object?>? Invoker;
    public override Type Type { get; }

    public override object? Get()
    {
        return Getter?.Invoke();
    }

    public override void Set(object? value)
    {
        Setter?.Invoke(value);
    }

    public override object? Invoke(object?[]? parameters)
    {
        return Invoker?.Invoke(parameters);
    }

    public override Type[] InvokeParameterTypes { get; }
    public override uint InvokeOptionalParameters { get; }
    public override Type InvokeReturnType { get; }
}
