using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Client.MapEditor.Interface.Panels.Helpers;

internal sealed class MapViewportContainer : ViewportContainer
{
    [Dependency] private readonly IOverlayManager _overlayManager = null!;

    private Vector2? _dragStartPos;
    private Vector2 _dragStartWorldPos;

    public IEye? Eye { get; set; }

    public event Action<Vector2>? WorldPosChanged;

    private readonly GridBackgroundOverlay.Parameters _gridParameters = new();

    public MapViewportContainer()
    {
        IoCManager.InjectDependencies(this);

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

    protected override void Resized()
    {
        // TODO: The way this state is managed sucks. Wish I could set overlays on a per-viewport basis sanely.
        if (Viewport != null && _overlayManager.TryGetOverlay(out GridBackgroundOverlay? bg))
        {
            bg.ViewportParameters.Remove(Viewport.Id);
        }

        base.Resized();

        if (Viewport != null && _overlayManager.TryGetOverlay(out bg))
        {
            bg.ViewportParameters[Viewport.Id] = _gridParameters;
        }
    }
}
