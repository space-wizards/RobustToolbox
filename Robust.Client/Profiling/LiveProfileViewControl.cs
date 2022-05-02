using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Profiling;

namespace Robust.Client.Profiling;

public sealed class LiveProfileViewControl : Control
{
    [Dependency] private readonly ProfManager _profManager = default!;

    public int MaxDepth { get; set; } = 2;
    public long? UseIndex;

    public ProfBuffer Buffer;
    public bool UseBuffer;

    public LiveProfileViewControl()
    {
        IoCManager.InjectDependencies(this);
    }

    protected internal override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (!_profManager.IsEnabled)
            return;

        var font = UserInterfaceManager.ThemeDefaults.DefaultFont;

        ref var buffer = ref UseBuffer ? ref Buffer : ref _profManager.Buffer;
        var baseLine = new Vector2(0, font.GetAscent(UIScale));

        var index = buffer.IndexIdx(UseIndex ?? buffer.IndexWriteOffset - 1);
        var i = index.StartPos;

        DrawCmds(ref buffer, ref i, ref baseLine, in index, 0, font, handle);
    }

    private void DrawCmds(
        ref ProfBuffer list,
        ref long i,
        ref Vector2 baseline,
        in ProfIndex index,
        int depth,
        Font font,
        DrawingHandleScreen handle)
    {
        for (; i < index.EndPos; i++)
        {
            ref var cmd = ref list.BufferIdx(i);
            DrawCmd(ref list, ref i, ref baseline, index, ref cmd, depth, font, handle);
        }
    }

    private void DrawCmdSample(
        ref ProfLogValue value,
        ref Vector2 baseline,
        Font font,
        DrawingHandleScreen handle)
    {
        DrawSample(baseline, value.StringId, value.Value, font, handle);

        baseline += (0, font.GetLineHeight(UIScale));
    }

    private void DrawSample(
        Vector2 baseline,
        int stringId,
        in ProfValue value,
        Font font,
        DrawingHandleScreen handle)
    {
        var cmdString = _profManager.GetString(stringId);
        baseline += handle.DrawString(font, baseline, cmdString, UIScale, Color.White);
        baseline += handle.DrawString(font, baseline, ": ", UIScale, Color.White);

        var str = value.Type switch
        {
            ProfValueType.TimeAllocSample =>
                $"{value.TimeAllocSample.Time * 1000:N2} ms, {value.TimeAllocSample.Alloc} B",
            ProfValueType.Int32 => value.Int32.ToString(),
            ProfValueType.Int64 => value.Int64.ToString(),
            _ => "???"
        };

        handle.DrawString(font, baseline, str, UIScale, Color.White);
    }

    private void DrawCmd(
        ref ProfBuffer buffer,
        ref long i,
        ref Vector2 baseline,
        in ProfIndex index,
        ref ProfLog log,
        int depth,
        Font font,
        DrawingHandleScreen handle)
    {
        switch (log.Type)
        {
            case ProfLogType.Sample:
                DrawCmdSample(ref log.Value, ref baseline, font, handle);
                break;
            case ProfLogType.GroupStart:
                DrawEnterGroup(ref buffer, ref i, ref baseline, index, depth, font, handle);
                break;
        }
    }

    private void DrawEnterGroup(
        ref ProfBuffer buffer,
        ref long i,
        ref Vector2 baseline,
        in ProfIndex index,
        int depth,
        Font font,
        DrawingHandleScreen handle)
    {
        depth += 1;

        var indentSize = 12 * UIScale;

        var startBaseline = baseline;
        baseline += (indentSize, font.GetLineHeight(UIScale));

        if (depth > MaxDepth)
        {
            var startIdx = i;

            // Skip contents of this group.
            for (; i < index.EndPos; i++)
            {
                ref var cmd = ref buffer.BufferIdx(i);
                if (cmd.Type != ProfLogType.GroupEnd)
                    continue;

                if (cmd.GroupEnd.StartIndex <= startIdx)
                    break;
            }
        }
        else
        {
            i += 1;

            for (; i < index.EndPos; i++)
            {
                ref var cmd = ref buffer.BufferIdx(i);
                if (cmd.Type == ProfLogType.GroupEnd)
                    break;

                DrawCmd(ref buffer, ref i, ref baseline, index, ref cmd, depth, font, handle);
            }
        }

        // Gone through entire list = unmatched begin/end pair
        if (i == index.EndPos)
            return;

        ref var cmdEnd = ref buffer.BufferIdx(i).GroupEnd;

        baseline -= (indentSize, 0);

        DrawSample(startBaseline, cmdEnd.StringId, cmdEnd.Value, font, handle);
    }
}
