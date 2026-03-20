using System;
using System.Collections.Generic;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Prototypes.PronounGrammar;
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

    /// <summary>
    ///     Optional list of custom pronouns for an entity, as well as the pronounGrammar inflection they belong to.
    ///     If this list does not contain a pronoun for a desired inflection,
    ///     the gender's pronoun will be used as a fallback.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<PronounGrammarPrototype>, string> Pronouns = [];

    [ViewVariables]
    public bool? ProperNoun
    {
        get => Attributes.TryGetValue("proper", out var g) ? bool.Parse(g) : null;
        [Obsolete("Use GrammarSystem.SetProperNoun instead")]
        set => IoCManager.Resolve<IEntityManager>().System<GrammarSystem>().SetProperNoun((Owner, this), value);
    }
}
