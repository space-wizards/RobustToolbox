using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

public sealed class UIBox2iTypeParser : SpanLikeTypeParser<UIBox2i, int>
{
    public override int Elements => 4;
    public override UIBox2i Create(Span<int> elements)
    {
        return new UIBox2i(elements[0], elements[1], elements[2], elements[3]);
    }
}