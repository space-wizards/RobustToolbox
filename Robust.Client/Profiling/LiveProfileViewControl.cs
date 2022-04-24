using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Profiling;
using Robust.Shared.Utility.Collections;

namespace Robust.Client.Profiling;

public sealed class LiveProfileViewControl : Control
{
    [Dependency] private readonly ProfManager _profManager = default!;

    public int MaxDepth = 1;

    public LiveProfileViewControl()
    {
        IoCManager.InjectDependencies(this);
    }

    protected internal override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var font = IoCManager.Resolve<IResourceCache>()
            .GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf")
            .MakeDefault();

        ref var list = ref _profManager.LastCmds();

        var baseLine = new Vector2(0, font.GetAscent(UIScale));
        var i = 0;

        DrawCmds(ref list, ref i, ref baseLine, 0, font, handle);
    }

    private void DrawCmds(
        ref ValueList<ProfCmd> list,
        ref int i,
        ref Vector2 baseline,
        int depth,
        Font font,
        DrawingHandleScreen handle)
    {
        for (; i < list.Count; i++)
        {
            ref var cmd = ref list[i];
            DrawCmd(ref list, ref i, ref baseline, ref cmd, depth, font, handle);
        }
    }

    private void DrawCmdSample(
        ref ProfCmdValue value,
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
        baseline += handle.DrawString(font, baseline, cmdString);
        baseline += handle.DrawString(font, baseline, ": ");

        var str = value.Type switch
        {
            ProfValueType.TimeAllocSample =>
                $"{value.TimeAllocSample.Time * 1000:N2} ms, {value.TimeAllocSample.Alloc} B",
            ProfValueType.Int32 => value.Int32.ToString(),
            _ => "???"
        };

        handle.DrawString(font, baseline, str);
    }

    private void DrawCmd(ref ValueList<ProfCmd> list,
        ref int i,
        ref Vector2 baseline,
        ref ProfCmd cmd,
        int depth,
        Font font,
        DrawingHandleScreen handle)
    {
        switch (cmd.Type)
        {
            case ProfCmdType.Sample:
                DrawCmdSample(ref cmd.Value, ref baseline, font, handle);
                break;
            case ProfCmdType.GroupStart:
                DrawEnterGroup(ref list, ref i, ref baseline, depth, font, handle);
                break;
        }
    }

    private void DrawEnterGroup(
        ref ValueList<ProfCmd> list,
        ref int i,
        ref Vector2 baseline,
        int depth,
        Font font,
        DrawingHandleScreen handle)
    {
        var indentSize = 12 * UIScale;

        var startBaseline = baseline;
        baseline += (indentSize, font.GetLineHeight(UIScale));

        if (depth > MaxDepth)
        {
            var startIdx = i;

            // Skip contents of this group.
            for (; i < list.Count; i++)
            {
                ref var cmd = ref list[i];
                if (cmd.Type != ProfCmdType.GroupEnd)
                    continue;

                if (cmd.GroupEnd.StartIndex <= startIdx)
                    break;
            }
        }
        else
        {
            i += 1;

            for (; i < list.Count; i++)
            {
                ref var cmd = ref list[i];
                if (cmd.Type == ProfCmdType.GroupEnd)
                    break;

                DrawCmd(ref list, ref i, ref baseline, ref cmd, depth + 1, font, handle);
            }
        }

        // Gone through entire list = unmatched begin/end pair
        if (i == list.Count)
            return;

        ref var cmdEnd = ref list[i].GroupEnd;

        baseline -= (indentSize, 0);

        DrawSample(startBaseline, cmdEnd.StringId, cmdEnd.Value, font, handle);
    }
}
