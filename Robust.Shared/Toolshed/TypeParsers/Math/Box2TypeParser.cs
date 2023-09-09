using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

public sealed class Box2TypeParser : SpanLikeTypeParser<Box2, float>
{
    public override int Elements => 4;
    public override Box2 Create(Span<float> elements)
    {
        return new Box2(elements[0], elements[1], elements[2], elements[3]);
    }
}
