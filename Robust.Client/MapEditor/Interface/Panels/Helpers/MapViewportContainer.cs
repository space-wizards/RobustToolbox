using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Graphics;
using Robust.Shared.Timing;

namespace Robust.Client.MapEditor.Interface.Panels.Helpers;

internal sealed class MapViewportContainer : ViewportContainer
{
    public IEye? Eye { get; set; }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (Viewport != null)
            Viewport.Eye = Eye;
    }
}
