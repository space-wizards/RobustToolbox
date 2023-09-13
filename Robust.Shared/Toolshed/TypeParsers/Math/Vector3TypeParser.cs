using System;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

internal sealed class Vector3TypeParser : SpanLikeTypeParser<Maths.Vector3, float>
{
    public override int Elements => 3;
    public override Maths.Vector3 Create(Span<float> elements)
    {
        return new Maths.Vector3(elements[0], elements[1], elements[2]);
    }
}