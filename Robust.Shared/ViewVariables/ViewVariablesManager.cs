using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;

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

    private const BindingFlags MembersBindings =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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

            if (typeof(EntityUid).IsAssignableFrom(type))
            {
                traits.Add(ViewVariablesTraits.Entity);
            }
        }

        return traits;
    }
}
