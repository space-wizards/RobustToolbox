using System;

namespace Robust.Shared.ViewVariables;

public interface IViewVariablesManager
{
    void RegisterDomain(string domain, DomainResolveObject resolveObject);
    bool UnregisterDomain(string domain);
    void RegisterTypeHandler<T>(HandleTypePath<T> handler, ListTypeCustomPaths<T> list);
    void RegisterTypeHandler(Type type, HandleTypePath handler, ListTypeCustomPaths list);
    bool UnregisterTypeHandler<T>();
    bool UnregisterTypeHandler(Type type);

    ViewVariablesPath? ResolvePath(string path);
    object? ReadPath(string path);
    void WritePath(string path, string value);
    object? InvokePath(string path, string arguments);
    string[] ListPath(string path);
}
