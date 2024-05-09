using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;

namespace Robust.Shared.GameObjects;

public sealed class GrammarSystem : EntitySystem
{
    public void Clear(Entity<GrammarComponent> grammar)
    {
        grammar.Comp.Attributes.Clear();
        Dirty(grammar);
    }

    public bool TryGet(Entity<GrammarComponent> grammar, string key, [NotNullWhen(true)] out string? value)
    {
        return grammar.Comp.Attributes.TryGetValue(key, out value);
    }

    public void Set(Entity<GrammarComponent> grammar, string key, string value)
    {
        grammar.Comp.Attributes[key] = value;
        Dirty(grammar);
    }

    public void SetGender(Entity<GrammarComponent> grammar, Gender? gender)
    {
        if (gender.HasValue)
            grammar.Comp.Attributes["gender"] = gender.Value.ToString();
        else
            grammar.Comp.Attributes.Remove("gender");

        Dirty(grammar);
    }

    public void SetProperNoun(Entity<GrammarComponent> grammar, bool? proper)
    {
        if (proper.HasValue)
            grammar.Comp.Attributes["proper"] = proper.Value.ToString();
        else
            grammar.Comp.Attributes.Remove("proper");

        Dirty(grammar);
    }
}
