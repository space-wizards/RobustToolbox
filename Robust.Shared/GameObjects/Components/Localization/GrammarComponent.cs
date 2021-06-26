using System;
using System.Collections.Generic;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components.Localization
{
    /// <summary>
    ///     Overrides grammar attributes specified in prototypes or localization files.
    /// </summary>
    [RegisterComponent]
    [NetID()]
    public class GrammarComponent : Component
    {
        public override string Name => "Grammar";

        [ViewVariables]
        [DataField("attributes")]
        public Dictionary<string, string> Attributes { get; } = new();

        [ViewVariables]
        public Gender? Gender
        {
            get => Attributes.TryGetValue("gender", out var g) ? Enum.Parse<Gender>(g, true) : null;
            set
            {
                if (value.HasValue)
                    Attributes["gender"] = value.Value.ToString();
                else
                    Attributes.Remove("gender");
            }
        }

        [ViewVariables]
        public bool? ProperNoun
        {
            get => Attributes.TryGetValue("proper", out var g) ? bool.Parse(g) : null;
            set
            {
                if (value.HasValue)
                    Attributes["proper"] = value.Value.ToString();
                else
                    Attributes.Remove("proper");
            }
        }
    }
}
