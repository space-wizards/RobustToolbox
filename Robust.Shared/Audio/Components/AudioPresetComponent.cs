using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Shared.Audio.Components;

/// <summary>
/// Marks this entity as being spawned for audio presets in case we need to reload.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), Access(typeof(SharedAudioSystem))]
public sealed partial class AudioPresetComponent : Component
{
    [AutoNetworkedField]
    public string Preset = string.Empty;
}
