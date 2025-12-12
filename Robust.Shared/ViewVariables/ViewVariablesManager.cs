using System;
using System.Collections;
using System.Collections.Generic;
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

internal abstract partial class ViewVariablesManager : IViewVariablesManager, IPostInjectInit
{
    [Dependency] private readonly ISerializationManager _serMan = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IReflectionManager _reflectionMan = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly ILogManager _logMan = default!;

    private readonly Dictionary<Type, HashSet<object>> _cachedTraits = new();

    private const BindingFlags MembersBindings =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    protected ISawmill Sawmill = default!;

    public virtual void Initialize()
    {
        InitializeDomains();
        InitializeTypeHandlers();
        InitializeRemote();
    }

    public object? ReadPath(string path)
    {
        return ResolvePath(path)?.Get();
    }

    public string? ReadPathSerialized(string path)
    {
        if (ResolvePath(path) is not {} p)
            return null;

        var obj = p.Get();

        if (obj == null)
            return "null";

        // This will throw if the object cannot be serialized, resort to ToString if so.
        try
        {
            return SerializeValue(p.Type, obj);
        }
        catch (Exception)
        {
            return obj.ToString();
        }
    }

    public void WritePath(string path, string value)
    {
        var resPath = ResolvePath(path);
        resPath?.Set(DeserializeValue(resPath.Type, value));
    }

    public object? InvokePath(string path, string arguments)
    {
        var resPath = ResolvePath(path);

        if (resPath == null)
            return null;

        var args = ParseArguments(arguments);

        var desArgs =
            DeserializeArguments(resPath.InvokeParameterTypes, (int)resPath.InvokeOptionalParameters, args);

        return resPath.Invoke(desArgs);
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

            if (typeof(NetEntity).IsAssignableFrom(type))
            {
                traits.Add(ViewVariablesTraits.Entity);
            }
        }

        return traits;
    }

    void IPostInjectInit.PostInject()
    {
        Sawmill = _logMan.GetSawmill("vv");
    }
}
