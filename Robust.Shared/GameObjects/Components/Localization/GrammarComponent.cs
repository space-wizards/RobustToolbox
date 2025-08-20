using System;
using System.Collections.Generic;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components.Localization;

/// <summary>
///     Overrides grammar attributes specified in prototypes or localization files.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
// [Access(typeof(GrammarSystem))] TODO access
public sealed partial class GrammarComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, string> Attributes = new();

    [ViewVariables]
    public Gender? Gender
    {
        get => Attributes.TryGetValue("gender", out var g) ? Enum.Parse<Gender>(g, true) : null;
        [Obsolete("Use GrammarSystem.SetGender instead")]
        set => IoCManager.Resolve<IEntityManager>().System<GrammarSystem>().SetGender((Owner, this), value);
    }

    [DataField]
    public Pronoun? Pronoun { get; set; }

    [ViewVariables]
    public bool? ProperNoun
    {
        get => Attributes.TryGetValue("proper", out var g) ? bool.Parse(g) : null;
        [Obsolete("Use GrammarSystem.SetProperNoun instead")]
        set => IoCManager.Resolve<IEntityManager>().System<GrammarSystem>().SetProperNoun((Owner, this), value);
    }
}

/// <summary>
///     Used to define custom pronouns for an entity.
///     If this doesn't exist, just use pronouns defined by the entity's gender.
/// </summary>
[DataDefinition, Serializable]
public sealed partial class Pronoun
{
    /// <summary>
    ///     Subject form of pronoun.
    ///     eg. "I think SHE is very nice".
    /// </summary>
    [DataField]
    public string? Subject;

    /// <summary>
    ///     Object form of pronoun.
    ///     eg. "I met HER recently".
    /// </summary>
    [DataField]
    public string? Object;

    /// <summary>
    ///     Dative form of pronoun.
    ///     Not used in en-US.
    ///     eg. "to him", "for her"".
    /// </summary>
    [DataField]
    public string? DatObj;

    /// <summary>
    ///     Genitive form of pronoun.
    ///     Not used in en-US.
    ///     eg. "у него", "seines Vaters".
    /// </summary>
    [DataField]
    public string? Genitive;

    /// <summary>
    ///     Possesive adjective / determiner form of pronoun.
    ///     eg. "Is this HER dog?".
    /// </summary>
    [DataField]
    public string? PossAdj;

    /// <summary>
    ///     Possessive pronoun form of pronoun.
    ///     eg. "She told me that the house is HERS".
    /// </summary>
    [DataField]
    public string? PossPronoun;

    /// <summary>
    ///     Reflexive form of pronoun.
    ///     eg. "She said she would rather do it HERSELF".
    /// </summary>
    [DataField]
    public string? Reflexive;

    /// <summary>
    ///     Counter word or measure word.
    ///     Not used in en-US.
    ///     eg. "两个人", "一本书".
    /// </summary>
    [DataField]
    public string? Counter;

    /// <summary>
    ///     When conjugating verbs, should we conjugate plurally?
    ///     eg. it IS / they ARE, it HAS / they HAVE, it RUNS / they RUN.
    /// </summary>
    [DataField]
    public bool? Plural;

    public Pronoun(string? subject,
    string? @object,
    string? datObj,
    string? genitive,
    string? possAdj,
    string? possPronoun,
    string? reflexive,
    string? counter,
    bool? plural)
    {
        Subject = subject;
        Object = @object;
        DatObj = datObj;
        Genitive = genitive;
        PossAdj = possAdj;
        PossPronoun = possPronoun;
        Reflexive = reflexive;
        Counter = counter;
        Plural = plural;
    }

    /// <summary>
    ///     Copy constructor
    /// </summary>
    public Pronoun(Pronoun other)
    : this(other.Subject,
        other.Object,
        other.DatObj,
        other.Genitive,
        other.PossAdj,
        other.PossPronoun,
        other.Reflexive,
        other.Counter,
        other.Plural)
    { }

    public Pronoun WithSubject(string? pronoun)
    {
        return new(this) { Subject = pronoun };
    }

    public Pronoun WithObject(string? pronoun)
    {
        return new(this) { Object = pronoun };
    }

    public Pronoun WithDatObj(string? pronoun)
    {
        return new(this) { DatObj = pronoun };
    }

    public Pronoun WithGenitive(string? pronoun)
    {
        return new(this) { Genitive = pronoun };
    }

    public Pronoun WithPossAdj(string? pronoun)
    {
        return new(this) { PossAdj = pronoun };
    }

    public Pronoun WithPossPronoun(string? pronoun)
    {
        return new(this) { PossPronoun = pronoun };
    }

    public Pronoun WithReflexive(string? pronoun)
    {
        return new(this) { Reflexive = pronoun };
    }

    public Pronoun WithCounter(string? pronoun)
    {
        return new(this) { Counter = pronoun };
    }

    public Pronoun WithPlural(bool? pronoun)
    {
        return new(this) { Plural = pronoun };
    }
}
