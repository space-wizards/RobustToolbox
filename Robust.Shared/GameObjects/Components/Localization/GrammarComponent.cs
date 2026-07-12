using System;
using System.Collections.Generic;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
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
    /// <summary>
    ///     Set values to overwrite Loc attributes (eg. proper, gender, pronouns)
    /// </summary>
    /// <remarks>
    ///     Use the id of a <see cref="PronounGrammarPrototype"> as a key to define custom pronouns per inflection.
    ///     If this list does not contain a pronoun for a desired inflection,
    ///     the gender's pronoun will be used as a fallback.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public Dictionary<string, string> Attributes = new();

    [ViewVariables]
    public Gender? Gender
    {
        get => Attributes.TryGetValue("gender", out var g) ? Enum.Parse<Gender>(g, true) : null;
        [Obsolete("Use GrammarSystem.SetGender instead")]
        set => IoCManager.Resolve<IEntityManager>().System<GrammarSystem>().SetGender((Owner, this), value);
    }

    [ViewVariables]
    public bool? ProperNoun
    {
        get => Attributes.TryGetValue("proper", out var g) ? bool.Parse(g) : null;
        [Obsolete("Use GrammarSystem.SetProperNoun instead")]
        set => IoCManager.Resolve<IEntityManager>().System<GrammarSystem>().SetProperNoun((Owner, this), value);
    }
}
