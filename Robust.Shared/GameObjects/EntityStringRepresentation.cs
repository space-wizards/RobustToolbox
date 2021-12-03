using Robust.Shared.Players;

namespace Robust.Shared.GameObjects;

/// <summary>
///     A type that represents an entity, and allows you to get a string containing human-readable information about it.
///     This type converts implicitly to string, for convenience purposes.
/// </summary>
/// <remarks>
///     This can be used to pretty-print information about entities and also to log various information regarding an
///     entity, if you're using string interpolation handlers.
/// </remarks>
/// <param name="Uid">The unique identifier of the entity.</param>
/// <param name="Deleted">Whether the entity has been deleted or not. Also true if the entity does not exist.</param>
/// <param name="Name">The name of the entity.</param>
/// <param name="Prototype">The prototype identifier of the entity, if any.</param>
/// <param name="Session">The session attached to the entity, if any.</param>
public readonly record struct EntityStringRepresentation
    (EntityUid Uid, bool Deleted, string? Name = null, string? Prototype = null, ICommonSession? Session = null)
{
    public override string ToString()
    {
        if (Deleted && Name == null)
            return $"{Uid}D";

        return $"{Name} ({Uid}{(Prototype != null ? $", {Prototype}" : "")}{(Session != null ? $", {Session.Name}" : "")}){(Deleted ? "D" : "")}";
    }
    
    public static implicit operator string(EntityStringRepresentation rep) => rep.ToString();
}
