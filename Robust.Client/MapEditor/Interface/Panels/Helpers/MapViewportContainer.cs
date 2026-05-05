using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Graphics;
using Robust.Shared.Timing;

namespace Robust.Client.MapEditor.Interface.Panels.Helpers;

internal sealed class MapViewportContainer : ViewportContainer
{
    private Vector2? _dragStartPos;
    private Vector2 _dragStartWorldPos;

    public IEye? Eye { get; set; }

    public event Action<Vector2>? WorldPosChanged;

    public MapViewportContainer()
    {
        MouseFilter = MouseFilterMode.Stop;
    }

    protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function != MapEditorKeyFunctions.ViewportDrag)
        {
            base.KeyBindDown(args);
            return;
        }

        if (Eye == null)
            return;

        _dragStartPos = args.RelativePosition;
        _dragStartWorldPos = Eye.Position.Position;
        args.Handle();
    }

    protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (args.Function != MapEditorKeyFunctions.ViewportDrag)
        {
            base.KeyBindUp(args);
            return;
        }

        _dragStartPos = null;
        args.Handle();
    }

    protected internal override void MouseMove(GUIMouseMoveEventArgs args)
    {
        if (_dragStartPos is not { } dragStart)
        {
            base.MouseMove(args);
            return;
        }

        var uiDiff = args.RelativePosition - dragStart;
        var worldDistance = uiDiff * new Vector2(-1, 1) / EyeManager.PixelsPerMeter;
        WorldPosChanged?.Invoke(_dragStartWorldPos + worldDistance);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (Viewport != null)
            Viewport.Eye = Eye;
    }
}
