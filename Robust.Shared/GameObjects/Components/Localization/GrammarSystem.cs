using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Shared.GameObjects;

public sealed partial class GrammarSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    private static readonly string GenderAttribute = "gender";
    private static readonly string ProperAttribute = "proper";

    public void Clear(Entity<GrammarComponent> grammar)
    {
        grammar.Comp.Attributes.Clear();
        Dirty(grammar);
    }

    public bool TryGet(Entity<GrammarComponent> grammar, string key, [NotNullWhen(true)] out string? value)
    {
        return grammar.Comp.Attributes.TryGetValue(key, out value);
    }

    /// <param name="ent"></param>
    /// <param name="pronouns">A dictionary of the entity's pronouns.</param>
    /// <returns>True if the entity has a <see cref="GrammarComponent"/>.</returns>
    public bool TryGetPronouns(Entity<GrammarComponent?> ent, [NotNullWhen(true)] out Dictionary<ProtoId<PronounGrammarPrototype>, string>? pronouns)
    {
        if (!Resolve(ent, ref ent.Comp))
        {
            pronouns = null;
            return false;
        }

        pronouns = [];
        foreach (var attribute in ent.Comp.Attributes)
        {
            if (_protoMan.TryIndex<PronounGrammarPrototype>(attribute.Key, out var inflection))
                pronouns.Add(inflection, attribute.Value);
        }
        return true;
    }

    public void Set(Entity<GrammarComponent> grammar, string? key, string? value, bool raiseEvents = true)
    {
        if (key == null)
            return;
        if (value == null || value == "")
            grammar.Comp.Attributes.Remove(key);
        else
            grammar.Comp.Attributes[key] = value;

        if (raiseEvents)
        {
            var ev = new EntityGrammarUpdatedEvent(grammar);
            RaiseLocalEvent(grammar, ref ev, true);
        }

        Dirty(grammar);
    }

    public void SetGender(Entity<GrammarComponent> grammar, Gender? gender, bool raiseEvents = true)
    {
        Set(grammar, GenderAttribute, gender?.ToString(), raiseEvents);
    }

    public void SetPronoun(Entity<GrammarComponent> grammar, KeyValuePair<PronounGrammarPrototype, string>? pronoun, bool raiseEvents = true)
    {
        Set(grammar, pronoun?.Key.ID.ToString(), pronoun?.Value, raiseEvents);
    }

    public void SetProperNoun(Entity<GrammarComponent> grammar, bool? proper, bool raiseEvents = true)
    {
        Set(grammar, ProperAttribute, proper?.ToString(), raiseEvents);
    }
}
