using Robust.Shared.Enums;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components.Localization
{
    [RegisterComponent]
    public class GrammarComponent : Component
    {
        public override string Name => "Grammar";
        public override uint? NetID => NetIDs.GRAMMAR;

        [ViewVariables]
        [DataField("localizationId")]
        public string LocalizationId = "";

        [ViewVariables]
        [DataField("gender")]
        public Gender? Gender = null;

        [ViewVariables]
        [DataField("proper")]
        public bool? ProperNoun = null;
    }
}
