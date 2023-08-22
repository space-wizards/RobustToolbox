using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Robust.Client.Audio;

/// <summary>
/// Debug overlay for audio.
/// </summary>
public sealed class AudioOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private AudioSystem _audio = default!;

    protected internal override void Draw(in OverlayDrawArgs args)
    {
        foreach (var stream in _audio.PlayingStreams)
        {
            // stream.
        }
    }
}
