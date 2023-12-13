using Robust.Shared.GameStates;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Synchronise the auto-animated layers of the sprite to real time;
/// this is useful if you require multiple entities to have synchronised animations.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SyncSpriteComponent : Component
{

}
