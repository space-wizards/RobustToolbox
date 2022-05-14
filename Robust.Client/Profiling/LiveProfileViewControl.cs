using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Profiling;

namespace Robust.Client.Profiling;

public sealed class LiveProfileViewControl : Control
{
    [Dependency] private readonly ProfManager _profManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public int MaxDepth { get; set; } = 2;

    public LiveProfileViewControl()
    {
        IoCManager.InjectDependencies(this);
    }

    protected internal override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (!_profManager.IsEnabled)
            return;

        var font = _resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf").MakeDefault();
        var baseLine = new Vector2(0, font.GetAscent(UIScale));

        ref readonly var buffer = ref _profManager.Buffer;
        ref readonly var index = ref buffer.Index(buffer.IndexWriteOffset - 1);

        DrawData drawData = default;
        drawData.Font = font;
        drawData.Buffer = buffer;
        drawData.Index = index;
        drawData.Handle = handle;

        var i = index.StartPos;

        DrawCmds(in drawData, ref i, ref baseLine, 0);
    }

    private void DrawCmds(
        in DrawData data,
        ref long i,
        ref Vector2 baseline,
        int depth)
    {
        for (; i < data.Index.EndPos; i++)
        {
            ref var cmd = ref data.Buffer.Log(i);
            DrawCmd(in data, ref i, ref baseline, depth, in cmd);
        }
    }

    private void DrawCmdSample(
        in DrawData data,
        ref Vector2 baseline,
        in ProfLogValue value)
    {
        DrawSample(in data, baseline, value.StringId, value.Value);

        baseline += (0, data.Font.GetLineHeight(UIScale));
    }

    private void DrawSample(
        in DrawData data,
        Vector2 baseline,
        int stringId,
        in ProfValue value)
    {
        var cmdString = _profManager.GetString(stringId);
        baseline += data.Handle.DrawString(data.Font, baseline, cmdString, UIScale, Color.White);
        baseline += data.Handle.DrawString(data.Font, baseline, ": ", UIScale, Color.White);

        var str = value.Type switch
        {
            ProfValueType.TimeAllocSample =>
                $"{value.TimeAllocSample.Time * 1000:N2} ms, {value.TimeAllocSample.Alloc} B",
            ProfValueType.Int32 => value.Int32.ToString(),
            ProfValueType.Int64 => value.Int64.ToString(),
            _ => "???"
        };

        data.Handle.DrawString(data.Font, baseline, str, UIScale, Color.White);
    }

    private void DrawCmd(
        in DrawData data,
        ref long i,
        ref Vector2 baseline,
        int depth,
        in ProfLog log)
    {
        switch (log.Type)
        {
            case ProfLogType.Value:
                DrawCmdSample(in data, ref baseline, in log.Value);
                break;
            case ProfLogType.GroupStart:
                DrawEnterGroup(in data, ref i, ref baseline, depth);
                break;
        }
    }

    private void DrawEnterGroup(
        in DrawData data,
        ref long i,
        ref Vector2 baseline,
        int depth)
    {
        depth += 1;

        var indentSize = 12 * UIScale;

        var startBaseline = baseline;
        baseline += (indentSize, data.Font.GetLineHeight(UIScale));

        if (depth > MaxDepth)
        {
            var startIdx = i;

            // Skip contents of this group.
            for (; i < data.Index.EndPos; i++)
            {
                ref var cmd = ref data.Buffer.Log(i);
                if (cmd.Type != ProfLogType.GroupEnd)
                    continue;

                if (cmd.GroupEnd.StartIndex <= startIdx)
                    break;
            }
        }
        else
        {
            i += 1;

            for (; i < data.Index.EndPos; i++)
            {
                ref var cmd = ref data.Buffer.Log(i);
                if (cmd.Type == ProfLogType.GroupEnd)
                    break;

                DrawCmd(in data, ref i, ref baseline, depth, in cmd);
            }
        }

        // Gone through entire list = unmatched begin/end pair
        if (i == data.Index.EndPos)
            return;

        ref var cmdEnd = ref data.Buffer.Log(i).GroupEnd;

        baseline -= (indentSize, 0);

        DrawSample(in data, startBaseline, cmdEnd.StringId, cmdEnd.Value);
    }

    private struct DrawData
    {
        public ProfBuffer Buffer;
        public ProfIndex Index;
        public Font Font;
        public DrawingHandleScreen Handle;
    }
}
