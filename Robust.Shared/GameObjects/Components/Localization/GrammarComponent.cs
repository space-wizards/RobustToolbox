
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
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

            if (serializer.TryReadDataFieldCached("gender", out string? gender0))
            {
                var refl = IoCManager.Resolve<IReflectionManager>();
                if (refl.TryParseEnumReference(gender0, out var gender))
                {
                    Gender = (Gender)gender;
                }
            }
            serializer.DataField(ref ProperNoun,     "proper",         null);
        }
    }
}
