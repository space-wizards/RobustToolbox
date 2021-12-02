using Robust.Shared.Players;

namespace Robust.Shared.GameObjects;

public readonly record struct EntityStringRepresentation
    (EntityUid Uid, bool Deleted, string? Name = null, string? Prototype = null, ICommonSession? Session = null)
{
    public override string ToString()
    {
        if (Deleted && Name == null)
            return $"{Uid}D";

        return $"{Name} ({Uid}{(Prototype != null ? $", {Prototype}" : "")}{(Session != null ? $", {Session.Name}" : "")}){(Deleted ? "D" : "")}";
    }

    public static implicit operator EntityUid(EntityStringRepresentation rep) => rep.Uid;
    public static implicit operator string(EntityStringRepresentation rep) => rep.ToString();
}
