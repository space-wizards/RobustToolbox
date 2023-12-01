using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Shared.Audio.Components;

/// <summary>
/// Flags a map as storing global audio entities.
/// This is to avoid leaving them in nullspace.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GlobalAudioMapComponent : Component
{

}
