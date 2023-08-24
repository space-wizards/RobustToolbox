using System;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

internal sealed class Vector4TypeParser : SpanLikeTypeParser<Maths.Vector4, float>
{
    public override int Elements => 4;
    public override Maths.Vector4 Create(Span<float> elements)
    {
        return new Maths.Vector4(elements[0], elements[1], elements[2], elements[4]);
    }
}