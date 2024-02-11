using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

public sealed class UIBox2TypeParser : SpanLikeTypeParser<UIBox2, float>
{
    public override int Elements => 4;
    public override UIBox2 Create(Span<float> elements)
    {
        return new UIBox2(elements[0], elements[1], elements[2], elements[3]);
    }
}