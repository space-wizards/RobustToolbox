using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

[NotYamlSerializable]
public record struct Entity<T> : IFluentEntityUid, IAsType<EntityUid>
    where T : IComponent?
{
    public EntityUid Owner;
    public T Comp;
    EntityUid IFluentEntityUid.FluentOwner => Owner;

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

    public EntityUid AsType() => Owner;

    public override int GetHashCode() => Owner.GetHashCode();
}

[NotYamlSerializable]
public record struct Entity<T1, T2> : IFluentEntityUid, IAsType<EntityUid>
    where T1 : IComponent? where T2 : IComponent?
{
    public EntityUid Owner;
    public T1 Comp1;
    public T2 Comp2;
    EntityUid IFluentEntityUid.FluentOwner => Owner;

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

    public static implicit operator Entity<T1, T2?>((EntityUid Owner, T1 Comp1) tuple)
    {
        return new Entity<T1, T2?>(tuple.Owner, tuple.Comp1, default);
    }

    public static implicit operator Entity<T1, T2?>(Entity<T1> ent)
    {
        return new Entity<T1, T2?>(ent.Owner, ent.Comp, default);
    }

    public static implicit operator Entity<T1>(Entity<T1, T2> ent)
    {
        return new Entity<T1>(ent.Owner, ent.Comp1);
    }

    public EntityUid AsType() => Owner;
}

[NotYamlSerializable]
public record struct Entity<T1, T2, T3> : IFluentEntityUid, IAsType<EntityUid>
    where T1 : IComponent? where T2 : IComponent? where T3 : IComponent?
{
    public EntityUid Owner;
    public T1 Comp1;
    public T2 Comp2;
    public T3 Comp3;
    EntityUid IFluentEntityUid.FluentOwner => Owner;

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

#region Partial Tuple Casts

    public static implicit operator Entity<T1, T2?, T3?>((EntityUid Owner, T1 Comp1) tuple)
    {
        return new Entity<T1, T2?, T3?>(tuple.Owner, tuple.Comp1, default, default);
    }

    public static implicit operator Entity<T1, T2, T3?>((EntityUid Owner, T1 Comp1, T2 Comp2) tuple)
    {
        return new Entity<T1, T2, T3?>(tuple.Owner, tuple.Comp1, tuple.Comp2, default);
    }

#endregion

#region Partial Entity Casts

    public static implicit operator Entity<T1, T2?, T3?>(Entity<T1> ent)
    {
        return new Entity<T1, T2?, T3?>(ent.Owner, ent.Comp, default, default);
    }

    public static implicit operator Entity<T1, T2, T3?>(Entity<T1, T2> ent)
    {
        return new Entity<T1, T2, T3?>(ent.Owner, ent.Comp1, ent.Comp2, default);
    }

#endregion

#region Entity Sub casts

    public static implicit operator Entity<T1>(Entity<T1, T2, T3> ent)
    {
        return new Entity<T1>(ent.Owner, ent.Comp1);
    }

    public static implicit operator Entity<T1, T2>(Entity<T1, T2, T3> ent)
    {
        return new Entity<T1, T2>(ent.Owner, ent.Comp1, ent.Comp2);
    }

#endregion

    public EntityUid AsType() => Owner;
}

[NotYamlSerializable]
public record struct Entity<T1, T2, T3, T4> : IFluentEntityUid, IAsType<EntityUid>
    where T1 : IComponent? where T2 : IComponent? where T3 : IComponent? where T4 : IComponent?
{
    public EntityUid Owner;
    public T1 Comp1;
    public T2 Comp2;
    public T3 Comp3;
    public T4 Comp4;
    EntityUid IFluentEntityUid.FluentOwner => Owner;

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

#region Partial Tuple Casts

    public static implicit operator Entity<T1, T2?, T3?, T4?>((EntityUid Owner, T1 Comp1) tuple)
    {
        return new Entity<T1, T2?, T3?, T4?>(tuple.Owner, tuple.Comp1, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3?, T4?>((EntityUid Owner, T1 Comp1, T2 Comp2) tuple)
    {
        return new Entity<T1, T2, T3?, T4?>(tuple.Owner, tuple.Comp1, tuple.Comp2, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3) tuple)
    {
        return new Entity<T1, T2, T3, T4?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, default);
    }

#endregion

#region Partial Entity Casts

    public static implicit operator Entity<T1, T2?, T3?, T4?>(Entity<T1> ent)
    {
        return new Entity<T1, T2?, T3?, T4?>(ent.Owner, ent.Comp, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3?, T4?>(Entity<T1, T2> ent)
    {
        return new Entity<T1, T2, T3?, T4?>(ent.Owner, ent.Comp1, ent.Comp2, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4?>(Entity<T1, T2, T3> ent)
    {
        return new Entity<T1, T2, T3, T4?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, default);
    }

#endregion

#region Entity Sub casts

    public static implicit operator Entity<T1>(Entity<T1, T2, T3, T4> ent)
    {
        return new Entity<T1>(ent.Owner, ent.Comp1);
    }

    public static implicit operator Entity<T1, T2>(Entity<T1, T2, T3, T4> ent)
    {
        return new Entity<T1, T2>(ent.Owner, ent.Comp1, ent.Comp2);
    }

    public static implicit operator Entity<T1, T2, T3>(Entity<T1, T2, T3, T4> ent)
    {
        return new Entity<T1, T2, T3>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3);
    }

#endregion

    public EntityUid AsType() => Owner;
}

[NotYamlSerializable]
public record struct Entity<T1, T2, T3, T4, T5> : IFluentEntityUid, IAsType<EntityUid>
    where T1 : IComponent? where T2 : IComponent? where T3 : IComponent? where T4 : IComponent? where T5 : IComponent?
{
    public EntityUid Owner;
    public T1 Comp1;
    public T2 Comp2;
    public T3 Comp3;
    public T4 Comp4;
    public T5 Comp5;
    EntityUid IFluentEntityUid.FluentOwner => Owner;

    public Entity(EntityUid owner, T1 comp1, T2 comp2, T3 comp3, T4 comp4, T5 comp5)
    {
        DebugTools.AssertOwner(owner, comp1);
        DebugTools.AssertOwner(owner, comp2);
        DebugTools.AssertOwner(owner, comp3);
        DebugTools.AssertOwner(owner, comp4);
        DebugTools.AssertOwner(owner, comp5);

        Owner = owner;
        Comp1 = comp1;
        Comp2 = comp2;
        Comp3 = comp3;
        Comp4 = comp4;
        Comp5 = comp5;
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4, T5 Comp5) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, tuple.Comp5);
    }

    public static implicit operator Entity<T1?, T2?, T3?, T4?, T5?>(EntityUid owner)
    {
        return new Entity<T1?, T2?, T3?, T4?, T5?>(owner, default, default, default, default, default);
    }

    public static implicit operator EntityUid(Entity<T1, T2, T3, T4, T5> ent)
    {
        return ent.Owner;
    }

    public static implicit operator T1(Entity<T1, T2, T3, T4, T5> ent)
    {
        return ent.Comp1;
    }

    public static implicit operator T2(Entity<T1, T2, T3, T4, T5> ent)
    {
        return ent.Comp2;
    }

    public static implicit operator T3(Entity<T1, T2, T3, T4, T5> ent)
    {
        return ent.Comp3;
    }

    public static implicit operator T4(Entity<T1, T2, T3, T4, T5> ent)
    {
        return ent.Comp4;
    }

    public static implicit operator T5(Entity<T1, T2, T3, T4, T5> ent)
    {
        return ent.Comp5;
    }

    public readonly void Deconstruct(out EntityUid owner, out T1 comp1, out T2 comp2, out T3 comp3, out T4 comp4, out T5 comp5)
    {
        owner = Owner;
        comp1 = Comp1;
        comp2 = Comp2;
        comp3 = Comp3;
        comp4 = Comp4;
        comp5 = Comp5;
    }

#region Partial Tuple Casts

    public static implicit operator Entity<T1, T2?, T3?, T4?, T5?>((EntityUid Owner, T1 Comp1) tuple)
    {
        return new Entity<T1, T2?, T3?, T4?, T5?>(tuple.Owner, tuple.Comp1, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3?, T4?, T5?>((EntityUid Owner, T1 Comp1, T2 Comp2) tuple)
    {
        return new Entity<T1, T2, T3?, T4?, T5?>(tuple.Owner, tuple.Comp1, tuple.Comp2, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4?, T5?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3) tuple)
    {
        return new Entity<T1, T2, T3, T4?, T5?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, default);
    }

#endregion

#region Partial Entity Casts

    public static implicit operator Entity<T1, T2?, T3?, T4?, T5?>(Entity<T1> ent)
    {
        return new Entity<T1, T2?, T3?, T4?, T5?>(ent.Owner, ent.Comp, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3?, T4?, T5?>(Entity<T1, T2> ent)
    {
        return new Entity<T1, T2, T3?, T4?, T5?>(ent.Owner, ent.Comp1, ent.Comp2, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4?, T5?>(Entity<T1, T2, T3> ent)
    {
        return new Entity<T1, T2, T3, T4?, T5?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5?>(Entity<T1, T2, T3, T4> ent)
    {
        return new Entity<T1, T2, T3, T4, T5?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, default);
    }

#endregion

#region Entity Sub casts

    public static implicit operator Entity<T1>(Entity<T1, T2, T3, T4, T5> ent)
    {
        return new Entity<T1>(ent.Owner, ent.Comp1);
    }

    public static implicit operator Entity<T1, T2>(Entity<T1, T2, T3, T4, T5> ent)
    {
        return new Entity<T1, T2>(ent.Owner, ent.Comp1, ent.Comp2);
    }

    public static implicit operator Entity<T1, T2, T3>(Entity<T1, T2, T3, T4, T5> ent)
    {
        return new Entity<T1, T2, T3>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3);
    }

    public static implicit operator Entity<T1, T2, T3, T4>(Entity<T1, T2, T3, T4, T5> ent)
    {
        return new Entity<T1, T2, T3, T4>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4);
    }

#endregion

    public EntityUid AsType() => Owner;
}

[NotYamlSerializable]
public record struct Entity<T1, T2, T3, T4, T5, T6> : IFluentEntityUid, IAsType<EntityUid>
    where T1 : IComponent? where T2 : IComponent? where T3 : IComponent? where T4 : IComponent? where T5 : IComponent? where T6 : IComponent?
{
    public EntityUid Owner;
    public T1 Comp1;
    public T2 Comp2;
    public T3 Comp3;
    public T4 Comp4;
    public T5 Comp5;
    public T6 Comp6;
    EntityUid IFluentEntityUid.FluentOwner => Owner;

    public Entity(EntityUid owner, T1 comp1, T2 comp2, T3 comp3, T4 comp4, T5 comp5, T6 comp6)
    {
        DebugTools.AssertOwner(owner, comp1);
        DebugTools.AssertOwner(owner, comp2);
        DebugTools.AssertOwner(owner, comp3);
        DebugTools.AssertOwner(owner, comp4);
        DebugTools.AssertOwner(owner, comp5);
        DebugTools.AssertOwner(owner, comp6);

        Owner = owner;
        Comp1 = comp1;
        Comp2 = comp2;
        Comp3 = comp3;
        Comp4 = comp4;
        Comp5 = comp5;
        Comp6 = comp6;
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4, T5 Comp5, T6 Comp6) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5, T6>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, tuple.Comp5, tuple.Comp6);
    }

    public static implicit operator Entity<T1?, T2?, T3?, T4?, T5?, T6?>(EntityUid owner)
    {
        return new Entity<T1?, T2?, T3?, T4?, T5?, T6?>(owner, default, default, default, default, default, default);
    }

    public static implicit operator EntityUid(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return ent.Owner;
    }

    public static implicit operator T1(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return ent.Comp1;
    }

    public static implicit operator T2(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return ent.Comp2;
    }

    public static implicit operator T3(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return ent.Comp3;
    }

    public static implicit operator T4(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return ent.Comp4;
    }

    public static implicit operator T5(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return ent.Comp5;
    }

    public static implicit operator T6(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return ent.Comp6;
    }

    public readonly void Deconstruct(out EntityUid owner, out T1 comp1, out T2 comp2, out T3 comp3, out T4 comp4, out T5 comp5, out T6 comp6)
    {
        owner = Owner;
        comp1 = Comp1;
        comp2 = Comp2;
        comp3 = Comp3;
        comp4 = Comp4;
        comp5 = Comp5;
        comp6 = Comp6;
    }

#region Partial Tuple Casts

    public static implicit operator Entity<T1, T2?, T3?, T4?, T5?, T6?>((EntityUid Owner, T1 Comp1) tuple)
    {
        return new Entity<T1, T2?, T3?, T4?, T5?, T6?>(tuple.Owner, tuple.Comp1, default, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3?, T4?, T5?, T6?>((EntityUid Owner, T1 Comp1, T2 Comp2) tuple)
    {
        return new Entity<T1, T2, T3?, T4?, T5?, T6?>(tuple.Owner, tuple.Comp1, tuple.Comp2, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4?, T5?, T6?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3) tuple)
    {
        return new Entity<T1, T2, T3, T4?, T5?, T6?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5?, T6?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5?, T6?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4, T5 Comp5) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5, T6?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, tuple.Comp5, default);
    }

#endregion

#region Partial Entity Casts

    public static implicit operator Entity<T1, T2?, T3?, T4?, T5?, T6?>(Entity<T1> ent)
    {
        return new Entity<T1, T2?, T3?, T4?, T5?, T6?>(ent.Owner, ent.Comp, default, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3?, T4?, T5?, T6?>(Entity<T1, T2> ent)
    {
        return new Entity<T1, T2, T3?, T4?, T5?, T6?>(ent.Owner, ent.Comp1, ent.Comp2, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4?, T5?, T6?>(Entity<T1, T2, T3> ent)
    {
        return new Entity<T1, T2, T3, T4?, T5?, T6?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5?, T6?>(Entity<T1, T2, T3, T4> ent)
    {
        return new Entity<T1, T2, T3, T4, T5?, T6?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6?>(Entity<T1, T2, T3, T4, T5> ent)
    {
        return new Entity<T1, T2, T3, T4, T5, T6?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5, default);
    }

#endregion

#region Entity Sub casts

    public static implicit operator Entity<T1>(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return new Entity<T1>(ent.Owner, ent.Comp1);
    }

    public static implicit operator Entity<T1, T2>(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return new Entity<T1, T2>(ent.Owner, ent.Comp1, ent.Comp2);
    }

    public static implicit operator Entity<T1, T2, T3>(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return new Entity<T1, T2, T3>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3);
    }

    public static implicit operator Entity<T1, T2, T3, T4>(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return new Entity<T1, T2, T3, T4>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5>(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return new Entity<T1, T2, T3, T4, T5>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5);
    }

#endregion

    public EntityUid AsType() => Owner;
}

[NotYamlSerializable]
public record struct Entity<T1, T2, T3, T4, T5, T6, T7> : IFluentEntityUid, IAsType<EntityUid>
    where T1 : IComponent? where T2 : IComponent? where T3 : IComponent? where T4 : IComponent? where T5 : IComponent? where T6 : IComponent? where T7 : IComponent?
{
    public EntityUid Owner;
    public T1 Comp1;
    public T2 Comp2;
    public T3 Comp3;
    public T4 Comp4;
    public T5 Comp5;
    public T6 Comp6;
    public T7 Comp7;
    EntityUid IFluentEntityUid.FluentOwner => Owner;

    public Entity(EntityUid owner, T1 comp1, T2 comp2, T3 comp3, T4 comp4, T5 comp5, T6 comp6, T7 comp7)
    {
        DebugTools.AssertOwner(owner, comp1);
        DebugTools.AssertOwner(owner, comp2);
        DebugTools.AssertOwner(owner, comp3);
        DebugTools.AssertOwner(owner, comp4);
        DebugTools.AssertOwner(owner, comp5);
        DebugTools.AssertOwner(owner, comp6);
        DebugTools.AssertOwner(owner, comp7);

        Owner = owner;
        Comp1 = comp1;
        Comp2 = comp2;
        Comp3 = comp3;
        Comp4 = comp4;
        Comp5 = comp5;
        Comp6 = comp6;
        Comp7 = comp7;
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6, T7>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4, T5 Comp5, T6 Comp6, T7 Comp7) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5, T6, T7>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, tuple.Comp5, tuple.Comp6, tuple.Comp7);
    }

    public static implicit operator Entity<T1?, T2?, T3?, T4?, T5?, T6?, T7?>(EntityUid owner)
    {
        return new Entity<T1?, T2?, T3?, T4?, T5?, T6?, T7?>(owner, default, default, default, default, default, default, default);
    }

    public static implicit operator EntityUid(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return ent.Owner;
    }

    public static implicit operator T1(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return ent.Comp1;
    }

    public static implicit operator T2(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return ent.Comp2;
    }

    public static implicit operator T3(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return ent.Comp3;
    }

    public static implicit operator T4(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return ent.Comp4;
    }

    public static implicit operator T5(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return ent.Comp5;
    }

    public static implicit operator T6(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return ent.Comp6;
    }

    public static implicit operator T7(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return ent.Comp7;
    }

    public readonly void Deconstruct(out EntityUid owner, out T1 comp1, out T2 comp2, out T3 comp3, out T4 comp4, out T5 comp5, out T6 comp6, out T7 comp7)
    {
        owner = Owner;
        comp1 = Comp1;
        comp2 = Comp2;
        comp3 = Comp3;
        comp4 = Comp4;
        comp5 = Comp5;
        comp6 = Comp6;
        comp7 = Comp7;
    }

#region Partial Tuple Casts

    public static implicit operator Entity<T1, T2?, T3?, T4?, T5?, T6?, T7?>((EntityUid Owner, T1 Comp1) tuple)
    {
        return new Entity<T1, T2?, T3?, T4?, T5?, T6?, T7?>(tuple.Owner, tuple.Comp1, default, default, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3?, T4?, T5?, T6?, T7?>((EntityUid Owner, T1 Comp1, T2 Comp2) tuple)
    {
        return new Entity<T1, T2, T3?, T4?, T5?, T6?, T7?>(tuple.Owner, tuple.Comp1, tuple.Comp2, default, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4?, T5?, T6?, T7?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3) tuple)
    {
        return new Entity<T1, T2, T3, T4?, T5?, T6?, T7?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5?, T6?, T7?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5?, T6?, T7?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6?, T7?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4, T5 Comp5) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5, T6?, T7?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, tuple.Comp5, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6, T7?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4, T5 Comp5, T6 Comp6) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5, T6, T7?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, tuple.Comp5, tuple.Comp6, default);
    }

#endregion

#region Partial Entity Casts

    public static implicit operator Entity<T1, T2?, T3?, T4?, T5?, T6?, T7?>(Entity<T1> ent)
    {
        return new Entity<T1, T2?, T3?, T4?, T5?, T6?, T7?>(ent.Owner, ent.Comp, default, default, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3?, T4?, T5?, T6?, T7?>(Entity<T1, T2> ent)
    {
        return new Entity<T1, T2, T3?, T4?, T5?, T6?, T7?>(ent.Owner, ent.Comp1, ent.Comp2, default, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4?, T5?, T6?, T7?>(Entity<T1, T2, T3> ent)
    {
        return new Entity<T1, T2, T3, T4?, T5?, T6?, T7?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5?, T6?, T7?>(Entity<T1, T2, T3, T4> ent)
    {
        return new Entity<T1, T2, T3, T4, T5?, T6?, T7?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6?, T7?>(Entity<T1, T2, T3, T4, T5> ent)
    {
        return new Entity<T1, T2, T3, T4, T5, T6?, T7?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6, T7?>(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return new Entity<T1, T2, T3, T4, T5, T6, T7?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5, ent.Comp6, default);
    }

#endregion

#region Entity Sub casts

    public static implicit operator Entity<T1>(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return new Entity<T1>(ent.Owner, ent.Comp1);
    }

    public static implicit operator Entity<T1, T2>(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return new Entity<T1, T2>(ent.Owner, ent.Comp1, ent.Comp2);
    }

    public static implicit operator Entity<T1, T2, T3>(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return new Entity<T1, T2, T3>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3);
    }

    public static implicit operator Entity<T1, T2, T3, T4>(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return new Entity<T1, T2, T3, T4>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5>(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return new Entity<T1, T2, T3, T4, T5>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6>(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return new Entity<T1, T2, T3, T4, T5, T6>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5, ent.Comp6);
    }

#endregion

    public EntityUid AsType() => Owner;

}

[NotYamlSerializable]
public record struct Entity<T1, T2, T3, T4, T5, T6, T7, T8> : IFluentEntityUid, IAsType<EntityUid>
    where T1 : IComponent? where T2 : IComponent? where T3 : IComponent? where T4 : IComponent? where T5 : IComponent? where T6 : IComponent? where T7 : IComponent? where T8 : IComponent?
{
    public EntityUid Owner;
    public T1 Comp1;
    public T2 Comp2;
    public T3 Comp3;
    public T4 Comp4;
    public T5 Comp5;
    public T6 Comp6;
    public T7 Comp7;
    public T8 Comp8;
    EntityUid IFluentEntityUid.FluentOwner => Owner;

    public Entity(EntityUid owner, T1 comp1, T2 comp2, T3 comp3, T4 comp4, T5 comp5, T6 comp6, T7 comp7, T8 comp8)
    {
        DebugTools.AssertOwner(owner, comp1);
        DebugTools.AssertOwner(owner, comp2);
        DebugTools.AssertOwner(owner, comp3);
        DebugTools.AssertOwner(owner, comp4);
        DebugTools.AssertOwner(owner, comp5);
        DebugTools.AssertOwner(owner, comp6);
        DebugTools.AssertOwner(owner, comp7);
        DebugTools.AssertOwner(owner, comp8);

        Owner = owner;
        Comp1 = comp1;
        Comp2 = comp2;
        Comp3 = comp3;
        Comp4 = comp4;
        Comp5 = comp5;
        Comp6 = comp6;
        Comp7 = comp7;
        Comp8 = comp8;
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6, T7, T8>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4, T5 Comp5, T6 Comp6, T7 Comp7, T8 Comp8) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5, T6, T7, T8>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, tuple.Comp5, tuple.Comp6, tuple.Comp7, tuple.Comp8);
    }

    public static implicit operator Entity<T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?>(EntityUid owner)
    {
        return new Entity<T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?>(owner, default, default, default, default, default, default, default, default);
    }

    public static implicit operator EntityUid(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return ent.Owner;
    }

    public static implicit operator T1(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return ent.Comp1;
    }

    public static implicit operator T2(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return ent.Comp2;
    }

    public static implicit operator T3(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return ent.Comp3;
    }

    public static implicit operator T4(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return ent.Comp4;
    }

    public static implicit operator T5(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return ent.Comp5;
    }

    public static implicit operator T6(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return ent.Comp6;
    }

    public static implicit operator T7(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return ent.Comp7;
    }

    public static implicit operator T8(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return ent.Comp8;
    }

    public readonly void Deconstruct(out EntityUid owner, out T1 comp1, out T2 comp2, out T3 comp3, out T4 comp4, out T5 comp5, out T6 comp6, out T7 comp7, out T8 comp8)
    {
        owner = Owner;
        comp1 = Comp1;
        comp2 = Comp2;
        comp3 = Comp3;
        comp4 = Comp4;
        comp5 = Comp5;
        comp6 = Comp6;
        comp7 = Comp7;
        comp8 = Comp8;
    }

#region Partial Tuple Casts

    public static implicit operator Entity<T1, T2?, T3?, T4?, T5?, T6?, T7?, T8?>((EntityUid Owner, T1 Comp1) tuple)
    {
        return new Entity<T1, T2?, T3?, T4?, T5?, T6?, T7?, T8?>(tuple.Owner, tuple.Comp1, default, default, default, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3?, T4?, T5?, T6?, T7?, T8?>((EntityUid Owner, T1 Comp1, T2 Comp2) tuple)
    {
        return new Entity<T1, T2, T3?, T4?, T5?, T6?, T7?, T8?>(tuple.Owner, tuple.Comp1, tuple.Comp2, default, default, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4?, T5?, T6?, T7?, T8?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3) tuple)
    {
        return new Entity<T1, T2, T3, T4?, T5?, T6?, T7?, T8?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, default, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5?, T6?, T7?, T8?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5?, T6?, T7?, T8?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6?, T7?, T8?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4, T5 Comp5) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5, T6?, T7?, T8?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, tuple.Comp5, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6, T7?, T8?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4, T5 Comp5, T6 Comp6) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5, T6, T7?, T8?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, tuple.Comp5, tuple.Comp6, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6, T7, T8?>((EntityUid Owner, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4, T5 Comp5, T6 Comp6, T7 Comp7) tuple)
    {
        return new Entity<T1, T2, T3, T4, T5, T6, T7, T8?>(tuple.Owner, tuple.Comp1, tuple.Comp2, tuple.Comp3, tuple.Comp4, tuple.Comp5, tuple.Comp6, tuple.Comp7, default);
    }

#endregion

#region Partial Entity Casts

    public static implicit operator Entity<T1, T2?, T3?, T4?, T5?, T6?, T7?, T8?>(Entity<T1> ent)
    {
        return new Entity<T1, T2?, T3?, T4?, T5?, T6?, T7?, T8?>(ent.Owner, ent.Comp, default, default, default, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3?, T4?, T5?, T6?, T7?, T8?>(Entity<T1, T2> ent)
    {
        return new Entity<T1, T2, T3?, T4?, T5?, T6?, T7?, T8?>(ent.Owner, ent.Comp1, ent.Comp2, default, default, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4?, T5?, T6?, T7?, T8?>(Entity<T1, T2, T3> ent)
    {
        return new Entity<T1, T2, T3, T4?, T5?, T6?, T7?, T8?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, default, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5?, T6?, T7?, T8?>(Entity<T1, T2, T3, T4> ent)
    {
        return new Entity<T1, T2, T3, T4, T5?, T6?, T7?, T8?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, default, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6?, T7?, T8?>(Entity<T1, T2, T3, T4, T5> ent)
    {
        return new Entity<T1, T2, T3, T4, T5, T6?, T7?, T8?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5, default, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6, T7?, T8?>(Entity<T1, T2, T3, T4, T5, T6> ent)
    {
        return new Entity<T1, T2, T3, T4, T5, T6, T7?, T8?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5, ent.Comp6, default, default);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6, T7, T8?>(Entity<T1, T2, T3, T4, T5, T6, T7> ent)
    {
        return new Entity<T1, T2, T3, T4, T5, T6, T7, T8?>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5, ent.Comp6, ent.Comp7, default);
    }

#endregion

#region Entity Sub casts

    public static implicit operator Entity<T1>(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return new Entity<T1>(ent.Owner, ent.Comp1);
    }

    public static implicit operator Entity<T1, T2>(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return new Entity<T1, T2>(ent.Owner, ent.Comp1, ent.Comp2);
    }

    public static implicit operator Entity<T1, T2, T3>(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return new Entity<T1, T2, T3>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3);
    }

    public static implicit operator Entity<T1, T2, T3, T4>(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return new Entity<T1, T2, T3, T4>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5>(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return new Entity<T1, T2, T3, T4, T5>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6>(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return new Entity<T1, T2, T3, T4, T5, T6>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5, ent.Comp6);
    }

    public static implicit operator Entity<T1, T2, T3, T4, T5, T6, T7>(Entity<T1, T2, T3, T4, T5, T6, T7, T8> ent)
    {
        return new Entity<T1, T2, T3, T4, T5, T6, T7>(ent.Owner, ent.Comp1, ent.Comp2, ent.Comp3, ent.Comp4, ent.Comp5, ent.Comp6, ent.Comp7);
    }

#endregion

    public EntityUid AsType() => Owner;
}
