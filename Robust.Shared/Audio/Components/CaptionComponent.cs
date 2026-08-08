using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;
using Robust.Shared.Physics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.GameStates;

namespace Robust.Shared.Audio.Components;

/// <summary>
/// Stores the caption data for an audio entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CaptionComponent : Component, IComponentTreeEntry<CaptionComponent>
{
    [DataField, AutoNetworkedField]
    public LocId? Caption { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public string? LocalizedCaption => Caption != null ? Loc.GetString(Caption) : null;

    public EntityUid? TreeUid { get; set; }

    public DynamicTree<ComponentTreeEntry<CaptionComponent>>? Tree { get; set; }

    public bool AddToTree => true;

    public bool TreeUpdateQueued { get; set; }
}
