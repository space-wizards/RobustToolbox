using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Robust.Shared.IoC;

namespace Robust.LanguageServer;

/// <summary>
/// Implementation of <see cref="IDependencyCollection"/> that copies registrations to a <see cref="IServiceCollection"/>.
/// </summary>
public sealed class ServiceDependencyCollection : IDependencyCollection
{
    private readonly IServiceCollection _collection;
    private readonly DependencyCollection _deps = new();

    public ServiceDependencyCollection(IServiceCollection collection)
    {
        _collection = collection;

        collection.AddSingleton<IDependencyCollection>(this);
    }

    public void Register<TInterface, TImplementation>(bool overwrite = false)
        where TImplementation : class, TInterface
        where TInterface : class
    {
        _deps.Register<TInterface, TImplementation>(overwrite);

        _collection.AddSingleton<TInterface>(_ => Resolve<TInterface>());
    }

    public void Register<TInterface, TImplementation>(
        DependencyFactoryDelegate<TImplementation> factory,
        bool overwrite = false)
        where TImplementation : class, TInterface
        where TInterface : class
    {
        _deps.Register<TInterface, TImplementation>(factory, overwrite);

        _collection.AddSingleton<TInterface>(_ => Resolve<TInterface>());
    }

    public void Register(
        Type implementation,
        DependencyFactoryDelegate<object>? factory = null,
        bool overwrite = false)
    {
        _deps.Register(implementation, factory, overwrite);

        _collection.AddSingleton(implementation, _ => ResolveType(implementation));
    }

    public void Register(
        Type interfaceType,
        Type implementation,
        DependencyFactoryDelegate<object>? factory = null,
        bool overwrite = false)
    {
        _deps.Register(interfaceType, implementation, factory, overwrite);

        _collection.AddSingleton(interfaceType, _ => ResolveType(interfaceType));
    }

    public void RegisterInstance<TInterface>(object implementation, bool overwrite = false, bool deferInject = false)
        where TInterface : class
    {
        _deps.RegisterInstance<TInterface>(implementation, overwrite, deferInject);

        _collection.AddSingleton((TInterface)implementation);
    }

    public void RegisterInstance(Type type, object implementation, bool overwrite = false, bool deferInject = false)
    {
        _deps.RegisterInstance(type, implementation, overwrite, deferInject);

        _collection.AddSingleton(type, _ => ResolveType(type));
    }

    public void Clear()
    {
        _deps.Clear();
    }

    public T Resolve<T>()
    {
        return _deps.Resolve<T>();
    }

    public void Resolve<T>([NotNull]ref T? instance)
    {
        _deps.Resolve(ref instance);
    }

    public void Resolve<T1, T2>([NotNull]ref T1? instance1, [NotNull]ref T2? instance2)
    {
        _deps.Resolve(ref instance1, ref instance2);
    }

    public void Resolve<T1, T2, T3>([NotNull]ref T1? instance1, [NotNull]ref T2? instance2, [NotNull]ref T3? instance3)
    {
        _deps.Resolve(ref instance1, ref instance2, ref instance3);
    }

    public void Resolve<T1, T2, T3, T4>(
        [NotNull]ref T1? instance1,
        [NotNull]ref T2? instance2,
        [NotNull]ref T3? instance3,
        [NotNull]ref T4? instance4)
    {
        _deps.Resolve(ref instance1, ref instance2, ref instance3, ref instance4);
    }

    public object ResolveType(Type type)
    {
        return _deps.ResolveType(type);
    }

    public bool TryResolveType<T>([NotNullWhen(true)] out T? instance)
    {
        return _deps.TryResolveType(out instance);
    }

    public bool TryResolveType(Type objectType, [MaybeNullWhen(false)] out object instance)
    {
        return _deps.TryResolveType(objectType, out instance);
    }

    public void BuildGraph()
    {
        _deps.BuildGraph();
    }

    public void InjectDependencies(object obj, bool oneOff = false)
    {
        _deps.InjectDependencies(obj, oneOff);
    }
}
