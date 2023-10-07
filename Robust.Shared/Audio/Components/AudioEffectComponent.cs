using Robust.Shared.Audio.Effects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Shared.Audio.Components;

/// <summary>
/// Stores OpenAL audio effect data that can be bound to an <see cref="AudioAuxiliaryComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AudioEffectComponent : Component
{
    internal IAudioEffect Effect = default!;
}
