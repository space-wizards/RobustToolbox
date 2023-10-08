using Robust.Shared.Audio.Effects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Audio.Components;

/// <summary>
/// Can have Audio passed to it to apply effects or filters.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedAudioSystem))]
public sealed partial class AudioAuxiliaryComponent : Component
{
    /// <summary>
    /// Audio effect to attach to this auxiliary audio slot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Effect;

    [ViewVariables]
    internal IAuxiliaryAudio Auxiliary = new DummyAuxiliaryAudio();
}
