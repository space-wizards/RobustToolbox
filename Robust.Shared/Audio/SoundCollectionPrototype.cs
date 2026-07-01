using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Robust.Shared.Localization;

namespace Robust.Shared.Audio;

[Prototype]
public sealed partial class SoundCollectionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LocId? Caption { get; private set; }

    [DataField("files")]
    public List<ResPath> PickFiles { get; private set; } = new();
}
