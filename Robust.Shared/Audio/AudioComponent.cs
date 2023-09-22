using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Shared.Audio;

/// <summary>
/// Stores the audio data for an audio entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AudioComponent : Component
{
    public IPlayingAudioStream Stream = default!;

    public AudioType AudioType = AudioType.Local;

    [AutoNetworkedField]
    public string FileName;

    [AutoNetworkedField]
    public AudioParams Params = AudioParams.Default;
}

public enum AudioType : byte
{
    /// <summary>
    /// Audio will have its position set to the parent entity every frame.
    /// </summary>
    Local,

    /// <summary>
    /// Audio will have its position set to the listener every frame.
    /// </summary>
    Global,
}
