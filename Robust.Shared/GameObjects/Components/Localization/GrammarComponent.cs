
using Robust.Shared.Enums;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components.Localization
{
    [RegisterComponent]
    public class GrammarComponent : Component
    {
        public override string Name => "Grammar";
        public override uint? NetID => NetIDs.GRAMMAR;

        [ViewVariables]
        public string LocalizationId = "";

        [ViewVariables]
        public Gender? Gender = null;

        [ViewVariables]
        public bool? ProperNoun = null;

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref LocalizationId, "localizationId", "");
            serializer.DataField(ref Gender,         "gender",         null);
            serializer.DataField(ref ProperNoun,     "proper",         null);
        }
    }
}
