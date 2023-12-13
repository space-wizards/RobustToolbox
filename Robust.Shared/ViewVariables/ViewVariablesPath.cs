using System;
using System.Linq;
using System.Reflection;
using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

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

    /// <summary>
    ///     Points to nearest parent component path. Will be null if none of the parent/base paths correspond to
    ///     components.
    /// </summary>
    public ViewVariablesComponentPath? ParentComponent;

    #region Static helper methods

    /// <summary>
    ///     Creates a <see cref="ViewVariablesFakePath"/> given an object.
    /// </summary>
    public static ViewVariablesFakePath FromObject(object obj)
        => new(() => obj, null, null, obj.GetType());

    /// <summary>
    ///     Creates a <see cref="ViewVariablesFakePath"/> given a getter function.
    /// </summary>
    public static ViewVariablesFakePath FromGetter(Func<object?> getter, Type type)
        => new(getter, null, null, type);

    /// <summary>
    ///     Creates a <see cref="ViewVariablesFakePath"/> given a setter function.
    /// </summary>
    public static ViewVariablesFakePath FromSetter(Action<object?> setter, Type type)
        => new(null, setter, null, type);

    /// <summary>
    ///     Creates a <see cref="ViewVariablesFakePath"/> given a function to be invoked.
    /// </summary>
    public static ViewVariablesFakePath FromInvoker(Func<object?, object?> invoker,
        Type[]? invokeParameterTypes = null, uint invokeOptionalParameters = 0, Type? invokeReturnType = null)
        => new(null, null, invoker, null, invokeParameterTypes, invokeOptionalParameters, invokeReturnType);

    /// <summary>
    ///     Creates a <see cref="ViewVariablesFakePath"/> given a function to be invoked.
    /// </summary>
    public static ViewVariablesFakePath FromInvoker(Action<object?> invoker,
        Type[]? invokeParameterTypes = null, uint invokeOptionalParameters = 0, Type? invokeReturnType = null)
        => new(null, null, invoker, null, invokeParameterTypes, invokeOptionalParameters, invokeReturnType);

    #endregion
}

internal sealed class ViewVariablesFieldOrPropertyPath : ViewVariablesPath
{
    private readonly IEntityManager _entMan;

    internal ViewVariablesFieldOrPropertyPath(object? obj, MemberInfo member, IEntityManager entMan)
    {
        if (member is not (FieldInfo or PropertyInfo))
            throw new ArgumentException("Member must be either a field or a property!", nameof(member));

        _object = obj;
        _member = member;
        _entMan = entMan;
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

        if (ParentComponent == null)
        {
            DebugTools.Assert(_object is not Component);
            return;
        }

        DebugTools.Assert(_object is not Component || ReferenceEquals(ParentComponent.Component, _object));
        _entMan.Dirty(ParentComponent.Owner, ParentComponent.Component);
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

public sealed class ViewVariablesComponentPath : ViewVariablesPath
{
    public readonly IComponent Component;
    public readonly EntityUid Owner;
    public override Type Type => Component?.GetType() ?? typeof(void);

    public ViewVariablesComponentPath(IComponent component, EntityUid owner)
    {
        Component = component;
        Owner = owner;
    }

    public override object? Get()
    {
        return Component;
    }

    public override void Set(object? value) { }
    public override object? Invoke(object?[]? parameters) => null;
}

public sealed class ViewVariablesFakePath : ViewVariablesPath
{
    public ViewVariablesFakePath(Func<object?>? getter, Action<object?>? setter, Func<object?, object?>? invoker = null,
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
    public override Type[] InvokeParameterTypes { get; }
    public override uint InvokeOptionalParameters { get; }
    public override Type InvokeReturnType { get; }

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

    public ViewVariablesFakePath WithGetter(Func<object?> getter, Type? type = null)
        => new(getter, _setter, _invoker, type ?? Type, InvokeParameterTypes, InvokeOptionalParameters, InvokeReturnType);

    public ViewVariablesFakePath WithSetter(Action<object?> setter, Type? type = null)
        => new(_getter, setter, _invoker, type ?? Type, InvokeParameterTypes, InvokeOptionalParameters, InvokeReturnType);

    public ViewVariablesFakePath WithInvoker(Func<object?, object?> invoker,
        Type[]? invokeParameterTypes = null, uint invokeOptionalParameters = 0, Type? invokeReturnType = null)
        => new(_getter, _setter, invoker, Type, invokeParameterTypes, invokeOptionalParameters,
            invokeReturnType);
}
