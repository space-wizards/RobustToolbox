using Robust.Shared.Enums;
using System;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Provides api for modifying <see cref="GrammarComponent"/> and helpers for common usecases.
/// Missing <c>GrammarComponent</c> is not logged to keep logs clean as it is a generic component.
/// </summary>
public sealed class GrammarSystem : EntitySystem
{
    /// <summary>
    /// Sets an attribute to a value, creating <c>GrammarComponent</c> if it does not exist.
    /// If the value is null the attribute is removed.
    /// </summary>
    /// <returns>Whether the attribute was set, even if to the same value</returns>
    public bool SetAttribute(Entity<GrammarComponent?> ent, string name, string? value)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        var attributes = ent.Comp.Attributes;
        if (value is {} attribute)
            attributes[name] = attribute;
        else
            attributes.Remove(name);

        Dirty(ent, ent.Comp);
        return true;
    }

    /// <summary>
    /// Gets an attribute's value, or null if it is not set.
    /// </summary>
    public string? GetAttribute(Entity<GrammarComponent?> ent, string name)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        ent.Comp.Attributes.TryGetValue(name, out string? value);
        return value;
    }

    /// <summary>
    /// Copies attributes from a source to a destination.
    /// Clears the destination then copies each attribute from source to it.
    /// The source does not get modified.
    /// </summary>
    /// <returns>Whether the attributes were copied</returns>
    public bool CopyAttributes(Entity<GrammarComponent?> source, Entity<GrammarComponent?> dest)
    {
        if (!Resolve(source, ref source.Comp, false) || !Resolve(dest, ref dest.Comp, false))
            return false;

        dest.Comp.Attributes.Clear();

        foreach (var (k, v) in source.Comp.Attributes)
        {
            dest.Comp.Attributes.Add(k, v);
        }

        Dirty(dest, dest.Comp);
        return true;
    }

    #region Gender and proper nouns

    /// <summary>
    /// Set the gender of an entity.
    /// </summary>
    public bool SetGender(Entity<GrammarComponent?> ent, Gender? gender)
    {
        return SetAttribute(ent, "gender", gender?.ToString());
    }

    /// <summary>
    /// Get the gender of an entity, or null if it is not set.
    /// </summary>
    public Gender? GetGender(Entity<GrammarComponent?> ent)
    {
        if (GetAttribute(ent, "gender") is not {} gender)
            return null;

        return Enum.Parse<Gender>(gender, true);
    }

    /// <summary>
    /// Set whether the entity's name is a proper noun or not.
    /// </summary>
    public bool SetProperNoun(Entity<GrammarComponent?> ent, bool? proper)
    {
        return SetAttribute(ent, "proper", proper?.ToString());
    }

    /// <summary>
    /// Make the entity's name a proper noun.
    /// </summary>
    public bool MakeProperNoun(Entity<GrammarComponent?> ent)
    {
        return SetProperNoun(ent, true);
    }

    /// <summary>
    /// Make the entity's name a common noun.
    /// </summary>
    public bool MakeCommonNoun(Entity<GrammarComponent?> ent)
    {
        return SetProperNoun(ent, false);
    }

    /// <summary>
    /// Get the proper noun-ness of an entity, or null if it is not set.
    /// </summary>
    public bool? GetProperNoun(Entity<GrammarComponent?> ent)
    {
        if (GetAttribute(ent, "proper") is not {} proper)
            return null;

        return bool.Parse(proper);
    }

    /// <summary>
    /// Returns true if the entity's name is a proper noun.
    /// </summary>
    public bool IsProperNoun(Entity<GrammarComponent?> ent)
    {
        return GetProperNoun(ent) ?? false;
    }

    #endregion
}
