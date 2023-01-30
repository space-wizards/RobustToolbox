using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Robust.Shared.ViewVariables;

internal abstract partial class ViewVariablesManager : IViewVariablesManager
{
    [Dependency] private readonly ISerializationManager _serMan = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IReflectionManager _reflectionMan = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    private readonly Dictionary<Type, HashSet<object>> _cachedTraits = new();
    private ViewVariablesSerializationContext _context = default!;
    private const BindingFlags MembersBindings =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public virtual void Initialize()
    {
        _context = new ViewVariablesSerializationContext();
        InitializeDomains();
        InitializeTypeHandlers();
        InitializeRemote();
    }

    public bool TryReadPath(string path, out object? result, out string? error)
    {
        if (TryResolvePath(path, out var resPath, out error))
        {
            result = resPath?.Get();
            return true;
        }

        result = null;
        return false;
    }

    public object? ReadPath(string path)
    {
        TryReadPath(path, out var result, out _);
        return result;
    }

    public bool TryReadPathSerialized(string path, out string? result, out string? error)
    {
        result = null;
        if (!TryResolvePath(path, out var p, out error))
            return false;

        var obj = p.Get();

        if (obj == null)
        {
            result = "null";
            return true;
        }

        // This will throw if the object cannot be serialized, resort to ToString if so.
        try
        {
            result = SerializeValue(p.Type, obj);
        }
        catch (Exception)
        {
            result = obj.ToString();
        }
        return true;
    }

    public bool TryWritePath(string path, string value, out string? error)
    {
        if (!TryResolvePath(path, out var resPath, out error))
            return false;

        if (TryDeserializeValue(resPath.Type, value, out var result, out error))
            resPath.Set(result);

        return false;
    }

    public bool TryInvokePath(string path, string arguments, out object? result, out string? error)
    {
        result = null;
        if (!TryResolvePath(path, out var resPath, out error))
            return false;

        var args = ParseArguments(arguments);

        if (!TryDeserializeArguments(resPath.InvokeParameterTypes, (int)resPath.InvokeOptionalParameters, args, out var deserializedArgs, out error))
            return false;

        result = resPath.Invoke(deserializedArgs);
        return true;
    }

    /// <summary>
    ///     Figures out which VV traits an object type has. This method is in shared so the client and server agree on this mess.
    /// </summary>
    /// <seealso cref="ViewVariablesBlobMetadata.Traits"/>
    public ICollection<object> TraitIdsFor(Type type)
    {
        if (!_cachedTraits.TryGetValue(type, out var traits))
        {
            traits = new HashSet<object>();
            _cachedTraits.Add(type, traits);
            if (ViewVariablesUtility.TypeHasVisibleMembers(type))
            {
                traits.Add(ViewVariablesTraits.Members);
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                traits.Add(ViewVariablesTraits.Enumerable);
            }

            if (typeof(EntityUid).IsAssignableFrom(type))
            {
                traits.Add(ViewVariablesTraits.Entity);
            }
        }

        return traits;
    }
}
