using System;
using Robust.Client.Graphics;
using Robust.Client.Profiling;
using Robust.Client.ResourceManagement;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Profiling;

namespace Robust.Client.UserInterface;

public sealed class ProfGraphView : Control
{
    private ProfViewManager.Snapshot? _snapshot;

    public event Action<long>? FrameSelected;

    private bool _dragging;

    public long HighlightFrame;

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
        var frame = (int)(MathHelper.Clamp(pos.X / Width, 0, 1)* trackedFrameCount);

        FrameSelected?.Invoke(frame + _snapshot.StartFrame);
    }

    protected internal override void Draw(DrawingHandleScreen handle)
    {
        if (_snapshot == null)
            return;

        var trackedFrameCount = _snapshot.EndFrame - _snapshot.StartFrame + 1;

        ref var buffer = ref _snapshot.Buffer;

        var x = (float) PixelWidth;
        var controlHeight = PixelHeight;
        var barWidth = PixelWidth / (float) trackedFrameCount;

        var frame = _snapshot.EndFrame;

        for (var i = _snapshot.Buffer.IndexWriteOffset - 1; i >= _snapshot.LowestValidIndex; i--)
        {
            ref var index = ref buffer.IndexIdx(i);
            var endPos = index.EndPos;

            ref var endGroup = ref buffer.BufferIdx(endPos - 1);
            if (endGroup.Type != ProfLogType.GroupEnd ||
                endGroup.GroupEnd.Value.Type != ProfValueType.TimeAllocSample)
                continue;

            var valueSeconds = endGroup.GroupEnd.Value.TimeAllocSample.Time;
            const float peak = 0.016f * 4;

            var height = controlHeight * (valueSeconds / peak);

            var rect = UIBox2.FromDimensions(x, (controlHeight - height), barWidth, height);
            var color = HighlightFrame == frame ? Color.Pink : Color.Yellow;
            handle.DrawRect(rect, color);

            x -= barWidth;

            frame -= 1;
        }

        var font = IoCManager.Resolve<IResourceCache>()
            .GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf")
            .MakeDefault();

        handle.DrawString(font, new Vector2(19, 19), $"{trackedFrameCount}", Color.Black);
        handle.DrawString(font, new Vector2(20, 20), $"{trackedFrameCount}");
    }
}
