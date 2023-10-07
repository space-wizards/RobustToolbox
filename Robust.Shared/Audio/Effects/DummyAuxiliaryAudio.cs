namespace Robust.Shared.Audio.Effects;

/// <inheritdoc />
internal sealed class DummyAuxiliaryAudio : IAuxiliaryAudio
{
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public void SetEffect(IAudioEffect? effect)
    {
    }
}
