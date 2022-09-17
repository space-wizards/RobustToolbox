using System;
using System.Linq;
using System.Reflection;
using Robust.Shared.Reflection;

namespace Robust.Shared.ViewVariables;

/// <summary>
///     Represents a ViewVariables path. Allows you to "Get", "Set" or "Invoke" the path.
/// </summary>
[Virtual]
public abstract class ViewVariablesPath
{
    /// <summary>
    ///     The type that is both returned by the <see cref="Get"/> method and used by the <see cref="Set"/> method.
    /// </summary>
    public abstract Type Type { get; }

    /// <summary>
    ///     Gets the value of the path, if possible.
    /// </summary>
    /// <returns>The value of the path, or null. Same type as <see cref="Type"/>.</returns>
    public abstract object? Get();

    /// <summary>
    ///     Sets the value of the path, if possible.
    /// </summary>
    /// <param name="value">The new value to set the path to. Must be of the same type as <see cref="Type"/>.</param>
    public abstract void Set(object? value);

    /// <summary>
    ///     Invokes the path, if possible.
    /// </summary>
    /// <param name="parameters">The parameters that the function takes.</param>
    /// <returns>The object returned by invoking the function, or null.</returns>
    public abstract object? Invoke(object?[]? parameters);

    /// <summary>
    ///     The types of all parameters in the <see cref="Invoke"/> method.
    /// </summary>
    /// <seealso cref="InvokeOptionalParameters"/>
    public virtual Type[] InvokeParameterTypes { get; } = Array.Empty<Type>();

    /// <summary>
    ///     The number of optional parameters in the <see cref="Invoke"/> method, starting from the end of the array.
    /// </summary>
    /// <seealso cref="InvokeParameterTypes"/>
    public virtual uint InvokeOptionalParameters { get; } = 0;

    /// <summary>
    ///     The type of the object returned by the <see cref="Invoke"/> method, or <see cref="Void"/> if none.
    /// </summary>
    public virtual Type InvokeReturnType { get; } = typeof(void);
}

internal sealed class ViewVariablesFieldOrPropertyPath : ViewVariablesPath
{
    internal ViewVariablesFieldOrPropertyPath(object? obj, MemberInfo member)
    {
        if (member is not (FieldInfo or PropertyInfo))
            throw new ArgumentException("Member must be either a field or a property!", nameof(member));

        _object = obj;
        _member = member;
        ViewVariablesUtility.TryGetViewVariablesAccess(member, out _access);
    }

    private readonly object? _object;
    private readonly MemberInfo _member;
    private readonly VVAccess? _access;
    public override Type Type => _member.GetUnderlyingType();

    public override object? Get()
    {
        if (_access == null)
            return null;

        try
        {
            return _object != null
                ? _member.GetValue(_object)
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override void Set(object? value)
    {
        if (_access != VVAccess.ReadWrite)
            return;

        if (_object != null)
            _member.SetValue(_object, value);
    }

    public override object? Invoke(object?[]? parameters) => null;
}

internal sealed class ViewVariablesMethodPath : ViewVariablesPath
{
    internal ViewVariablesMethodPath(object? obj, MethodInfo method)
    {
        _object = obj;
        _method = method;
        ViewVariablesUtility.TryGetViewVariablesAccess(method, out _access);
    }

    private readonly object? _object;
    private readonly MethodInfo _method;
    private readonly VVAccess? _access;
    public override Type Type => typeof(void);
    public override Type InvokeReturnType => _method.ReturnType;

    public override object? Get() => null;

    public override void Set(object? value)
    {
    }

    public override object? Invoke(object?[]? parameters)
    {
        if (_access != VVAccess.ReadWrite)
            return null;

        return _object != null
            ? _method.Invoke(_object, parameters)
            : null;
    }

    public override Type[] InvokeParameterTypes
        => _access == VVAccess.ReadWrite
            ? _method.GetParameters().Select(info => info.ParameterType).ToArray()
            : Array.Empty<Type>();
    public override uint InvokeOptionalParameters
        => _access == VVAccess.ReadWrite
            ? (uint) _method.GetParameters().Count(info => info.IsOptional)
            : 0;
}

internal sealed class ViewVariablesIndexedPath : ViewVariablesPath
{
    internal ViewVariablesIndexedPath(object? obj, PropertyInfo indexer, object?[] index, VVAccess? parentAccess)
    {
        if (indexer.GetIndexParameters().Length == 0)
            throw new ArgumentException("PropertyInfo is not an indexer!", nameof(indexer));

        _object = obj;
        _indexer = indexer;
        _index = index;
        _access = parentAccess;
    }

    private readonly object? _object;
    private readonly PropertyInfo _indexer;
    private readonly object?[] _index;
    private readonly VVAccess? _access;
    public override Type Type => _indexer.GetUnderlyingType();

    public override object? Get()
    {
        if (_access == null)
            return null;

        try
        {
            return _object != null
                ? _indexer.GetValue(_object, _index)
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override void Set(object? value)
    {
        if(_access == VVAccess.ReadWrite && _object != null)
            _indexer.SetValue(_object, value, _index);
    }

    public override object? Invoke(object?[]? parameters) => null;
}

public sealed class ViewVariablesInstancePath : ViewVariablesPath
{
    public ViewVariablesInstancePath(object? obj)
    {
        _object = obj;
    }

    private readonly object? _object;

    public override Type Type => _object?.GetType() ?? typeof(void);

    public override object? Get() => _object;

    public override void Set(object? value)
    {
    }

    public override object? Invoke(object?[]? parameters) => null;
}

public sealed class ViewVariablesFakePath : ViewVariablesPath
{
    public ViewVariablesFakePath(Func<object?>? getter, Action<object?>? setter, Func<object?, object?>? invoker,
        Type? type = null, Type[]? invokeParameterTypes = null, uint invokeOptionalParameters = 0, Type? invokeReturnType = null)
    {
        _getter = getter;
        _setter = setter;
        _invoker = invoker;
        Type = type ?? typeof(void);
        InvokeParameterTypes = invokeParameterTypes ?? Array.Empty<Type>();
        InvokeOptionalParameters = invokeOptionalParameters;
        InvokeReturnType = invokeReturnType ?? typeof(void);
    }

    public ViewVariablesFakePath(Func<object?>? getter, Action<object?>? setter, Action<object?> invoker,
        Type? type = null, Type[]? invokeParameterTypes = null, uint invokeOptionalParameters = 0, Type? invokeReturnType = null)
        : this(getter, setter, null, type, invokeParameterTypes, invokeOptionalParameters, invokeReturnType)
    {
        _invoker = p =>
        {
            invoker(p);
            return null;
        };
    }

    private readonly Func<object?>? _getter;
    private readonly Action<object?>? _setter;
    private readonly Func<object?, object?>? _invoker;
    public override Type Type { get; }

    public override object? Get()
    {
        return _getter?.Invoke();
    }

    public override void Set(object? value)
    {
        _setter?.Invoke(value);
    }

    public override object? Invoke(object?[]? parameters)
    {
        return _invoker?.Invoke(parameters);
    }

    public override Type[] InvokeParameterTypes { get; }
    public override uint InvokeOptionalParameters { get; }
    public override Type InvokeReturnType { get; }
}
