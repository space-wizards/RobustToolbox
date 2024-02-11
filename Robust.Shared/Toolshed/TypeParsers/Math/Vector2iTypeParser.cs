using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

internal sealed class Vector2iTypeParser : SpanLikeTypeParser<Vector2i, int>
{
    public override int Elements => 2;
    public override Vector2i Create(Span<int> elements)
    {
        return new Vector2i(elements[0], elements[1]);
    }
}