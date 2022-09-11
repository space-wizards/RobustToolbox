using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Players;
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

    Task<string?> ReadRemotePath(string path, ICommonSession? session = null);
    Task WriteRemotePath(string path, string? value, ICommonSession? session = null);
    Task<string?> InvokeRemotePath(string path, string arguments, ICommonSession? session = null);
    Task<IEnumerable<string>> ListRemotePath(string path, VVListPathOptions options, ICommonSession? session = null);
}

// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public readonly struct VVListPathOptions
{
    public VVAccess MinimumAccess { get; init; }
    public bool ListIndexers { get; init; }
    public int RemoteListLength { get; init; }

    public VVListPathOptions()
    {
        MinimumAccess = VVAccess.ReadOnly;
        ListIndexers = true;
        RemoteListLength = ViewVariablesManager.MaxListPathResponseLength;
    }
}
