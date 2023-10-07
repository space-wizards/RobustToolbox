using Robust.Shared.Audio.Effects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Shared.Audio.Components;

/// <summary>
/// Can have Audio passed to it to apply effects or filters.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AudioAuxiliaryComponent : Component
{
    internal IAuxiliaryAudio Auxiliary = default!;
}
