using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Utility.TUnion;

[PublicAPI]
public readonly record struct None;

[PublicAPI]
public readonly record struct Deleted(EntityUid Target) : IError
{
    public string Describe()
    {
        return $"The entity {Target} was deleted or never existed.";
    }
}

/// <summary>
/// An error kind indicating that some entity is missing expected components.
/// </summary>
[PublicAPI]
public readonly record struct MissingComponents<T0>(EntityUid Target) : IError
    where T0: IComponent
{
    public string Describe()
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        var name0 = factory.GetComponentName(typeof(T0));
        return $"Couldn't get the component {name0} on {Target}.";
    }
}

/// <summary>
/// An error kind indicating that some entity is missing expected components.
/// </summary>
[PublicAPI]
public readonly record struct MissingComponents<T0, T1>(EntityUid Target) : IError
    where T0: IComponent
    where T1: IComponent
{
    public string Describe()
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        var name0 = factory.GetComponentName(typeof(T0));
        var name1 = factory.GetComponentName(typeof(T1));
        return $"Couldn't get the components {name0} and {name1} on {Target}.";
    }
}

/// <summary>
/// An error kind indicating that some entity is missing expected components.
/// </summary>
[PublicAPI]
public readonly record struct MissingComponents<T0, T1, T2>(EntityUid Target) : IError
    where T0: IComponent
    where T1: IComponent
    where T2: IComponent
{
    public string Describe()
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        var name0 = factory.GetComponentName(typeof(T0));
        var name1 = factory.GetComponentName(typeof(T1));
        var name2 = factory.GetComponentName(typeof(T2));
        return $"Couldn't get the components {name0}, {name1}, and {name2} on {Target}.";
    }
}

/// <summary>
/// An error kind indicating that some entity is missing expected components.
/// </summary>
[PublicAPI]
public readonly record struct MissingComponents<T0, T1, T2, T3>(EntityUid Target) : IError
    where T0: IComponent
    where T1: IComponent
    where T2: IComponent
    where T3: IComponent
{
    public string Describe()
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        var name0 = factory.GetComponentName(typeof(T0));
        var name1 = factory.GetComponentName(typeof(T1));
        var name2 = factory.GetComponentName(typeof(T2));
        var name3 = factory.GetComponentName(typeof(T3));
        return $"Couldn't get the components {name0}, {name1}, {name2}, and {name3} on {Target}.";
    }
}

/// <summary>
/// An error kind indicating that no entities with the expected components exist.
/// </summary>
[PublicAPI]
public readonly record struct NoneWithComponents<T0> : IError
    where T0: IComponent
{
    public string Describe()
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        var name0 = factory.GetComponentName(typeof(T0));
        return $"Couldn't find any entity with the component {name0}.";
    }
}

/// <summary>
/// An error kind indicating that no entities with the expected components exist.
/// </summary>
[PublicAPI]
public readonly record struct NoneWithComponents<T0, T1> : IError
    where T0: IComponent
    where T1: IComponent
{
    public string Describe()
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        var name0 = factory.GetComponentName(typeof(T0));
        var name1 = factory.GetComponentName(typeof(T1));
        return $"Couldn't find any entity with the components {name0} and {name1}.";
    }
}

/// <summary>
/// An error kind indicating that no entities with the expected components exist.
/// </summary>
[PublicAPI]
public readonly record struct NoneWithComponents<T0, T1, T2> : IError
    where T0: IComponent
    where T1: IComponent
    where T2: IComponent
{
    public string Describe()
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        var name0 = factory.GetComponentName(typeof(T0));
        var name1 = factory.GetComponentName(typeof(T1));
        var name2 = factory.GetComponentName(typeof(T2));
        return $"Couldn't find any entity with the components {name0}, {name1}, and {name2}.";
    }
}

/// <summary>
/// An error kind indicating that no entities with the expected components exist.
/// </summary>
[PublicAPI]
public readonly record struct NoneWithComponents<T0, T1, T2, T3>(EntityUid Target) : IError
    where T0: IComponent
    where T1: IComponent
    where T2: IComponent
    where T3: IComponent
{
    public string Describe()
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        var name0 = factory.GetComponentName(typeof(T0));
        var name1 = factory.GetComponentName(typeof(T1));
        var name2 = factory.GetComponentName(typeof(T2));
        var name3 = factory.GetComponentName(typeof(T3));
        return $"Couldn't find any entity with the components {name0}, {name1}, {name2}, and {name3}.";
    }
}
