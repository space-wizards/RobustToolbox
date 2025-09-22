using System;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Physics;

/// <summary>
///     This is a data struct for use with a <see cref="DynamicTree"/>. This stores both some generic component and the
///     entity's transform component. This is being used in place of a simple tuple so that the IEquatable can be
///     overriden, such that we can remove entries without needing to fetch the transform component of a possible
///     deleted entity.
/// </summary>
public readonly struct ComponentTreeEntry<T> : IEquatable<ComponentTreeEntry<T>>, IComparable<ComponentTreeEntry<T>> where T : IComponent
{
    public T Component { get; init; }
    public TransformComponent Transform { get; init; }
    public EntityUid Uid => Component.Owner;

    public int CompareTo(ComponentTreeEntry<T> other)
    {
        return Uid.CompareTo(other.Uid);
    }

    public bool Equals(ComponentTreeEntry<T> other)
    {
        return Uid.Equals(other.Uid);
    }

    public readonly void Deconstruct(out T component, out TransformComponent xform)
    {
        component = Component;
        xform = Transform;
    }

    public static implicit operator ComponentTreeEntry<T>((T, TransformComponent) tuple)
    {
        return new ComponentTreeEntry<T>()
        {
            Component = tuple.Item1,
            Transform = tuple.Item2
        };
    }
}
