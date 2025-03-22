using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

[Prototype]
public sealed partial class FontPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("path", required: true)]
    public ResPath Path { get; private set; } = default!;
}
