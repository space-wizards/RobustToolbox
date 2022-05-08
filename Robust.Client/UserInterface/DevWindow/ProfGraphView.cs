using System;
using Robust.Client.Graphics;
using Robust.Client.Profiling;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Profiling;

namespace Robust.Client.UserInterface;

internal sealed class ProfGraphView : Control
{
    private const int HeightFps = 15;
    private const float MaxHeightMs = 1 / (float) HeightFps;

    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ProfViewManager.Snapshot? _snapshot;

    public event Action<long>? FrameSelected;

    private bool _dragging;

    public long HighlightFrame;

    public ProfGraphView()
    {
        RectClipContent = true;

        IoCManager.InjectDependencies(this);
    }

    public void LoadSnapshot(ProfViewManager.Snapshot snapshot)
    {
        _snapshot = snapshot;
    }

    protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _dragging = true;
        args.Handle();
        DoFrameSelect(args.RelativePosition);
    }

    protected internal override void MouseMove(GUIMouseMoveEventArgs args)
    {
        if (!_dragging)
            return;

        DoFrameSelect(args.RelativePosition);
    }

    protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        args.Handle();
        _dragging = false;
    }

    private void DoFrameSelect(Vector2 pos)
    {
        if (_snapshot == null)
            return;

        var trackedFrameCount = _snapshot.EndFrame - _snapshot.StartFrame + 1;
        var frame = MathHelper.Clamp((int)(pos.X / Width * trackedFrameCount), 0, trackedFrameCount-1);

        FrameSelected?.Invoke(frame + _snapshot.StartFrame);
    }

    protected internal override void Draw(DrawingHandleScreen handle)
    {
        if (_snapshot == null)
            return;

        var targetFps = _cfg.GetCVar(CVars.DebugTargetFps);

        var trackedFrameCount = _snapshot.EndFrame - _snapshot.StartFrame + 1;

        ref var buffer = ref _snapshot.Buffer;

        var barWidth = PixelWidth / (float) trackedFrameCount;
        var x = PixelWidth - barWidth;
        var controlHeight = PixelHeight;

        var frame = _snapshot.EndFrame;

        for (var i = _snapshot.Buffer.IndexWriteOffset - 1; i >= _snapshot.LowestValidIndex; i--)
        {
            var valueSeconds = GetFrameTime(buffer, buffer.Index(i)).Time;

            var height = controlHeight * (valueSeconds / MaxHeightMs);

            var rect = UIBox2.FromDimensions(x, (controlHeight - height), barWidth, height);
            var color = HighlightFrame == frame ? Color.Pink : FrameGraph.FrameTimeColor(valueSeconds, targetFps);
            handle.DrawRect(rect, color);

            x -= barWidth;

            frame -= 1;
        }
    }

    internal static TimeAndAllocSample GetFrameTime(in ProfBuffer buffer, in ProfIndex index)
    {
        var endPos = index.EndPos;

        ref var endGroup = ref buffer.Log(endPos - 1);
        if (endGroup.Type != ProfLogType.GroupEnd ||
            endGroup.GroupEnd.Value.Type != ProfValueType.TimeAllocSample)
            return default;

        return endGroup.GroupEnd.Value.TimeAllocSample;
    }
}
