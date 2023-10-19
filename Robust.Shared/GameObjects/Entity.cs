using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public record struct Entity<T>
    where T : IComponent?
{
    public EntityUid Owner;
    public T Comp;

    public Entity(EntityUid owner, T comp)
    {
        DebugTools.AssertOwner(owner, comp);
        Owner = owner;
        Comp = comp;
    }

    public static implicit operator Entity<T>((EntityUid Owner, T Comp) tuple)
    {
        return new Entity<T>(tuple.Owner, tuple.Comp);
    }

    public static implicit operator Entity<T?>(EntityUid owner)
    {
        return new Entity<T?>(owner, default);
    }

    public static implicit operator EntityUid(Entity<T> ent)
    {
        return ent.Owner;
    }

    public static implicit operator T(Entity<T> ent)
    {
        return ent.Comp;
    }

    public readonly void Deconstruct(out EntityUid owner, out T comp)
    {
        owner = Owner;
        comp = Comp;
    }
}

public record struct Entity<T1, T2>
    where T1 : IComponent? where T2 : IComponent?
{
    public EntityUid Owner;
    public T1 Comp1;
    public T2 Comp2;

    public Entity(EntityUid owner, T1 comp1, T2 comp2)
    {
        DebugTools.AssertOwner(owner, comp1);
        DebugTools.AssertOwner(owner, comp2);
        Owner = owner;
        Comp1 = comp1;
        Comp2 = comp2;
    }

    public static implicit operator Entity<T1, T2>((EntityUid Owner, T1 Comp1, T2 Comp2) tuple)
    {
        return new Entity<T1, T2>(tuple.Owner, tuple.Comp1, tuple.Comp2);
    }

    public static implicit operator Entity<T1?, T2?>(EntityUid owner)
    {
        return new Entity<T1?, T2?>(owner, default, default);
    }

    public static implicit operator EntityUid(Entity<T1, T2> ent)
    {
        return ent.Owner;
    }

    public static implicit operator T1(Entity<T1, T2> ent)
    {
        return ent.Comp1;
    }

    public static implicit operator T2(Entity<T1, T2> ent)
    {
        return ent.Comp2;
    }

    public readonly void Deconstruct(out EntityUid owner, out T1 comp1, out T2 comp2)
    {
        owner = Owner;
        comp1 = Comp1;
        comp2 = Comp2;
    }
}

public record struct Entity<T1, T2, T3>
    where T1 : IComponent? where T2 : IComponent? where T3 : IComponent?
{
    public EntityUid Owner;
    public T1 Comp1;
    public T2 Comp2;
    public T3 Comp3;

    public Entity(EntityUid owner, T1 comp1, T2 comp2, T3 comp3)
    {
        DebugTools.AssertOwner(owner, comp1);
        DebugTools.AssertOwner(owner, comp2);
        DebugTools.AssertOwner(owner, comp3);
        Owner = owner;
        Comp1 = comp1;
        Comp2 = comp2;
        Comp3 = comp3;
    }

    public static implicit operator Entity<T1, T2, T3>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3) tuple)
    {
        return new Entity<T1, T2, T3>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3);
    }

    public static implicit operator Entity<T1?, T2?, T3?>(EntityUid owner)
    {
        return new Entity<T1?, T2?, T3?>(owner, default, default, default);
    }

    public static implicit operator EntityUid(Entity<T1, T2, T3> ent)
    {
        return ent.Owner;
    }

    public static implicit operator T1(Entity<T1, T2, T3> ent)
    {
        return ent.Comp1;
    }

    public static implicit operator T2(Entity<T1, T2, T3> ent)
    {
        return ent.Comp2;
    }

    public static implicit operator T3(Entity<T1, T2, T3> ent)
    {
        return ent.Comp3;
    }

    public readonly void Deconstruct(out EntityUid owner, out T1 comp1, out T2 comp2, out T3 comp3)
    {
        owner = Owner;
        comp1 = Comp1;
        comp2 = Comp2;
        comp3 = Comp3;
    }
}

public record struct Entity<T1, T2, T3, T4>
    where T1 : IComponent? where T2 : IComponent? where T3 : IComponent? where T4 : IComponent?
{
    public EntityUid Owner;
    public T1 Comp1;
    public T2 Comp2;
    public T3 Comp3;
    public T4 Comp4;

    public Entity(EntityUid owner, T1 comp1, T2 comp2, T3 comp3, T4 comp4)
    {
        DebugTools.AssertOwner(owner, comp1);
        DebugTools.AssertOwner(owner, comp2);
        DebugTools.AssertOwner(owner, comp3);
        DebugTools.AssertOwner(owner, comp4);
        Owner = owner;
        Comp1 = comp1;
        Comp2 = comp2;
        Comp3 = comp3;
        Comp4 = comp4;
    }

    public static implicit operator Entity<T1, T2, T3, T4>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4) tuple)
    {
        return new Entity<T1, T2, T3, T4>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4);
    }

    public static implicit operator Entity<T1?, T2?, T3?, T4?>(EntityUid owner)
    {
        return new Entity<T1?, T2?, T3?, T4?>(owner, default, default, default, default);
    }

    public static implicit operator EntityUid(Entity<T1, T2, T3, T4> ent)
    {
        return ent.Owner;
    }

    public static implicit operator T1(Entity<T1, T2, T3, T4> ent)
    {
        return ent.Comp1;
    }

    public static implicit operator T2(Entity<T1, T2, T3, T4> ent)
    {
        return ent.Comp2;
    }

    public static implicit operator T3(Entity<T1, T2, T3, T4> ent)
    {
        return ent.Comp3;
    }

    public static implicit operator T4(Entity<T1, T2, T3, T4> ent)
    {
        return ent.Comp4;
    }

    public readonly void Deconstruct(out EntityUid owner, out T1 comp1, out T2 comp2, out T3 comp3, out T4 comp4)
    {
        owner = Owner;
        comp1 = Comp1;
        comp2 = Comp2;
        comp3 = Comp3;
        comp4 = Comp4;
    }
}
