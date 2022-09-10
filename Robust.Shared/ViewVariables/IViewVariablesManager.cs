using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Robust.Shared.ViewVariables;

public interface IViewVariablesManager
{
    void RegisterDomain(string domain, DomainResolveObject resolveObject, DomainListPaths list);
    bool UnregisterDomain(string domain);
    void RegisterTypeHandler<T>(HandleTypePath<T> handler, ListTypeCustomPaths<T> list);
    void RegisterTypeHandler(Type type, HandleTypePath handler, ListTypeCustomPaths list);
    bool UnregisterTypeHandler<T>();
    bool UnregisterTypeHandler(Type type);

    ViewVariablesPath? ResolvePath(string path);
    object? ReadPath(string path);
    void WritePath(string path, string value);
    object? InvokePath(string path, string arguments);
    IEnumerable<string> ListPath(string path, VVListPathOptions options);
}

// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public readonly struct VVListPathOptions
{
    public VVAccess MinimumAccess { get; init; }
    public bool ListIndexers { get; init; }

    public VVListPathOptions()
    {
        MinimumAccess = VVAccess.ReadOnly;
        ListIndexers = true;
    }
}
